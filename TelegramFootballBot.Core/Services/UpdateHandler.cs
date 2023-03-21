using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Exceptions;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core.Models;
using TelegramFootballBot.Core.Models.CallbackQueries;

namespace TelegramFootballBot.Core.Services
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly CommandFactory _commandFactory;
        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISheetService _sheetService;
        private readonly ILogger _logger;

        public UpdateHandler(CommandFactory commandFactory, IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger)
        {
            _commandFactory = commandFactory;
            _messageService = messageService;
            _playerRepository = playerRepository;
            _sheetService = sheetService;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
        {
            var handler = update switch
            {
                { Message: { } message } => BotOnMessageReceived(message),
                { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery),
                _ => UnknownUpdateHandlerAsync(update)
            };

            await handler;
        }

        private async Task BotOnMessageReceived(Message message)
        {
            var command = _commandFactory.Create(message);
            if (command == null)
                return;

            try
            {
                await command.ExecuteAsync(message);
                var playerName = await GetPlayerNameAsync(message.From!.Id);
                _logger.Information($"Command {message.Text} processed for user {playerName}");
            }
            catch (Exception ex)
            {
                var playerName = await GetPlayerNameAsync(message.From!.Id);
                _logger.Error(ex, $"Error on processing {message.Text} command for user {playerName}");
                await _messageService.SendMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
                await _messageService.SendErrorMessageToUserAsync(message.Chat.Id, playerName);
            }
        }

        private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
        {
            try
            {
                var callbackData = callbackQuery.Data;
                if (string.IsNullOrEmpty(callbackData))
                    return;

                if (Callback.GetCallbackName(callbackData) == PlayerSetCallback.Name)
                    await DetermineIfPlayerIsReadyToPlayAsync(callbackQuery);

                _logger.Information($"Processed callback: {callbackQuery.Data}");
            }
            catch (Exception ex)
            {
                await ProcessCallbackError(callbackQuery, ex);
            }
        }

        private Task UnknownUpdateHandlerAsync(Update update)
        {
            _logger.Information("Unknown update type: {UpdateType}", update.Type);
            return Task.CompletedTask;
        }

        private async Task<string> GetPlayerNameAsync(long userId)
        {
            try
            {
                return (await _playerRepository.GetAsync(userId)).Name;
            }
            catch (UserNotFoundException)
            {
                return string.Empty;
            }
        }

        private async Task DetermineIfPlayerIsReadyToPlayAsync(CallbackQuery callbackQuery)
        {
            var playerSetCallback = new PlayerSetCallback(callbackQuery.Data!);
            await ClearInlineKeyboardAsync(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId);

            try
            {
                await _messageService.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on deleting message");
            }

            if (IsButtonPressedAfterGame(playerSetCallback.GameDate))
            {
                _logger.Information($"Button pressed after game: now - {DateTime.UtcNow}, game date - {playerSetCallback.GameDate.Date}");
                return;
            }

            var player = await _playerRepository.GetAsync(callbackQuery.From.Id);
            await _sheetService.SetApproveCellAsync(player.Name, GetApproveCellValue(playerSetCallback.UserAnswer));

            var approvedPlayersMessage = await _sheetService.BuildApprovedPlayersMessageAsync();
            player.IsGoingToPlay = playerSetCallback.UserAnswer == Constants.YES_ANSWER;
            player.ApprovedPlayersMessageId = await SendApprovedPlayersMessageAsync(approvedPlayersMessage, callbackQuery.Message.Chat.Id, player);
            player.ApprovedPlayersMessage = approvedPlayersMessage;

            await _playerRepository.UpdateAsync(player);
        }

        /// <summary>
        /// Sends an approved players message if it wasn't sent yet. Otherwise edits it.
        /// </summary>
        /// <param name="chatId">Player chat id</param>
        /// <param name="player">Player</param>
        /// <returns>Sent message id</returns>
        private async Task<int> SendApprovedPlayersMessageAsync(string message, ChatId chatId, Player player)
        {
            if (player.ApprovedPlayersMessageId != 0)
                await _messageService.DeleteMessageAsync(chatId, player.ApprovedPlayersMessageId);

            var messageResponse = await _messageService.SendMessageAsync(chatId, message);
            return messageResponse.MessageId;
        }

        private static string GetApproveCellValue(string userAnswer)
        {
            return userAnswer switch
            {
                Constants.YES_ANSWER => "1",
                Constants.NO_ANSWER => "0",
                Constants.MAYBE_ANSWER => "0.5",
                _ => throw new ArgumentOutOfRangeException($"userAnswer: {userAnswer}"),
            };
        }

        private static bool IsButtonPressedAfterGame(DateTime gameDate)
        {
            return gameDate.Date < DateTime.Now.Date;
        }

        private async Task ClearInlineKeyboardAsync(ChatId chatId, int messageId)
        {
            try
            {
                await _messageService.ClearReplyMarkupAsync(chatId, messageId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on clearing inline keyboard");
            }
        }

        private async Task ProcessCallbackError(CallbackQuery callbackQuery, Exception ex)
        {
            var messageForUser = string.Empty;
            var messageForBotOwner = string.Empty;
            var userId = callbackQuery.From.Id;
            var player = ex is UserNotFoundException ? null : await _playerRepository.GetAsync(userId);

            if (ex is UserNotFoundException)
            {
                _logger.Error($"User with id {userId} not found. Name: {callbackQuery.From.FirstName} {callbackQuery.From.LastName}");
                messageForUser = "Вы не зарегистрированы. Введите команду /reg Фамилия Имя.";
                messageForBotOwner = $"Пользователь {callbackQuery.From.FirstName} {callbackQuery.From.LastName} не найден";
            }

            if (ex is TotalsRowNotFoundExeption)
            {
                _logger.Error("\"Всего\" row not found in excel-file");
                messageForUser = "Не найдена строка \"Всего\" в excel-файле.";
                messageForBotOwner = $"Не найдена строка \"Всего\" в excel-файле. Пользователь - {player!.Name}";
            }

            if (ex is OperationCanceledException)
            {
                _logger.Error($"Operation {callbackQuery.Data} cancelled for user {player!.Name}.");
                messageForUser = "Не удалось обработать запрос.";
                messageForBotOwner = $"Операция обработки ответа отменена для пользователя {player.Name}";
            }

            if (ex is ArgumentOutOfRangeException exception)
            {
                _logger.Error($"Unexpected response for user {player!.Name}: {exception.ParamName}");
                messageForUser = "Непредвиденный вариант ответа.";
                messageForBotOwner = $"Непредвиденный вариант ответа для пользователя {player.Name}";
            }

            if (messageForUser == string.Empty)
            {
                _logger.Error(ex, "Unexpected error");
                messageForUser = "Непредвиденная ошибка.";
                messageForBotOwner = $"Ошибка у пользователя {player!.Name}: {ex.Message}";
            }

            await NotifyAboutError(callbackQuery.Message!.Chat.Id, messageForUser, messageForBotOwner);
        }

        private async Task NotifyAboutError(ChatId chatId, string messageForUser, string messageForBotOwner)
        {
            await _messageService.SendMessageAsync(chatId, messageForUser);

            if (AppSettings.NotifyOwner)
                await _messageService.SendMessageToBotOwnerAsync(messageForBotOwner);
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.Error("Polling failed with exception: {Exception}", exception);
            return Task.CompletedTask;
        }
    }
}
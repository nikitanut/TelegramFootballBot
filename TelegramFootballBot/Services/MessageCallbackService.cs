using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using TelegramFootballBot.Data;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;
using TelegramFootballBot.Models.CallbackQueries;

namespace TelegramFootballBot.Services
{
    public class MessageCallbackService
    {
        private readonly CommandFactory _commandFactory;
        private readonly IMessageService _messageService;
        private readonly ITeamService _teamService;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISheetService _sheetService;
        private readonly ILogger _logger;

        public MessageCallbackService(CommandFactory commandFactory, IMessageService messageService, ITeamService teamsService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger)
        {
            _commandFactory = commandFactory;
            _messageService = messageService;
            _teamService = teamsService;
            _playerRepository = playerRepository;
            _sheetService = sheetService;
            _logger = logger;
        }

        public async void OnMessageRecievedAsync(object sender, MessageEventArgs e)
        {
            var command = _commandFactory.Create(e.Message);
            if (command == null)
                return;

            try
            {
                await command.Execute(e.Message);
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Information($"Command {e.Message.Text} processed for user {playerName}");
            }
            catch (Exception ex)
            {
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Error(ex, $"Error on processing {e.Message.Text} command for user {playerName}");
                await _messageService.SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
                await _messageService.SendErrorMessageToUserAsync(e.Message.Chat.Id, playerName);
            }
        }

        public async void OnCallbackQueryAsync(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                var callbackData = e.CallbackQuery.Data;
                if (string.IsNullOrEmpty(callbackData))
                    return;

                if (Callback.GetCallbackName(callbackData) == PlayerSetCallback.Name)
                    await DetermineIfUserIsReadyToPlayAsync(e.CallbackQuery);

                if (Callback.GetCallbackName(callbackData) == TeamPollCallback.Name)
                    await DetermineIfUserLikesTeamAsync(e.CallbackQuery);

                _logger.Information($"Processed callback: {e.CallbackQuery.Data}");
            }
            catch (Exception ex)
            {
                await ProcessCallbackError(e.CallbackQuery, ex);
            }
        }

        private async Task<string> GetPlayerNameAsync(int userId)
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

        private async Task DetermineIfUserIsReadyToPlayAsync(CallbackQuery callbackQuery)
        {
            var playerSetCallback = new PlayerSetCallback(callbackQuery.Data);
            await ClearInlineKeyboardAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);

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
            await _sheetService.UpdateApproveCellAsync(player.Name, GetApproveCellValue(playerSetCallback.UserAnswer));

            player.IsGoingToPlay = playerSetCallback.UserAnswer == Constants.YES_ANSWER;
            player.ApprovedPlayersMessageId = await SendApprovedPlayersMessageAsync(callbackQuery.Message.Chat.Id, player);

            await _playerRepository.UpdateAsync(player);
        }

        /// <summary>
        /// Sends an approved players message if it wasn't sent yet. Otherwise edits it.
        /// </summary>
        /// <param name="chatId">Player chat id</param>
        /// <param name="player">Player</param>
        /// <returns>Sent message id</returns>
        private async Task<int> SendApprovedPlayersMessageAsync(ChatId chatId, Player player)
        {
            var approvedPlayersMessage = await _sheetService.GetApprovedPlayersMessageAsync();

            if (player.ApprovedPlayersMessageId != 0)
            {
                try
                {
                    await _messageService.EditMessageTextAsync(chatId, player.ApprovedPlayersMessageId, approvedPlayersMessage);
                    return player.ApprovedPlayersMessageId;
                }
                catch (Exception ex) // Telegram API doesn't allow to check if user deleted message
                {
                    _logger.Error(ex, $"Error on editing message for user {player.Name}");
                    return (await _messageService.SendMessageAsync(chatId, approvedPlayersMessage)).MessageId;
                }
            }

            return (await _messageService.SendMessageAsync(chatId, approvedPlayersMessage)).MessageId;
        }

        private async Task DetermineIfUserLikesTeamAsync(CallbackQuery callbackQuery)
        {
            await ClearInlineKeyboardAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
            _teamService.ProcessPollChoice(new TeamPollCallback(callbackQuery.Data));
            
            var player = await _playerRepository.GetAsync(callbackQuery.From.Id);
            player.PollMessageId = await SendTeamPollMessageAsync(callbackQuery.Message.Chat.Id);
            await _playerRepository.UpdateAsync(player);
        }

        private async Task<int> SendTeamPollMessageAsync(ChatId chatId)
        {
            var message = _teamService.GetMessageWithLikes();
            return (await _messageService.SendMessageAsync(chatId, message)).MessageId;
        }

        private string GetApproveCellValue(string userAnswer)
        {
            switch (userAnswer)
            {
                case Constants.YES_ANSWER: return "1";
                case Constants.NO_ANSWER: return "0";
                case Constants.MAYBE_ANSWER: return "0.5";
                default:
                    throw new ArgumentOutOfRangeException($"userAnswer: {userAnswer}");
            }
        }

        private bool IsButtonPressedAfterGame(DateTime gameDate)
        {
            return gameDate.Date < DateTime.Now.Date;
        }

        private async Task ClearInlineKeyboardAsync(ChatId chatId, int messageId)
        {
            try
            {
                var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
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
                messageForBotOwner = $"Не найдена строка \"Всего\" в excel-файле. Пользователь - {player.Name}";
            }

            if (ex is OperationCanceledException)
            {
                _logger.Error($"Operation {callbackQuery.Data} cancelled for user {player.Name}.");
                messageForUser = "Не удалось обработать запрос.";
                messageForBotOwner = $"Операция обработки ответа отменена для пользователя {player.Name}";
            }

            if (ex is ArgumentOutOfRangeException)
            {
                _logger.Error($"Unexpected response for user {player.Name}: {((ArgumentOutOfRangeException)ex).ParamName}");
                messageForUser = "Непредвиденный вариант ответа.";
                messageForBotOwner = $"Непредвиденный вариант ответа для пользователя {player.Name}";
            }

            if (messageForUser == string.Empty)
            {
                _logger.Error(ex, "Unexpected error");
                messageForUser = "Непредвиденная ошибка.";
                messageForBotOwner = $"Ошибка у пользователя {player.Name}: {ex.Message}";
            }

            await NotifyAboutError(callbackQuery.Message.Chat.Id, messageForUser, messageForBotOwner);
        }

        private async Task NotifyAboutError(ChatId chatId, string messageForUser, string messageForBotOwner)
        {
            await _messageService.SendMessageAsync(chatId, messageForUser);

            if (AppSettings.NotifyOwner)
                await _messageService.SendTextMessageToBotOwnerAsync(messageForBotOwner);
        }
    }
}

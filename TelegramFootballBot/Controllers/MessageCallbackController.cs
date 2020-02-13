using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Data;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Controllers
{
    public class MessageCallbackController
    {
        private readonly TelegramBotClient _client;
        private readonly IPlayerRepository _playerRepository;
        private readonly ILogger _logger;

        public MessageCallbackController(TelegramBotClient client, IPlayerRepository playerRepository, ILogger logger)
        {
            _client = client;
            _playerRepository = playerRepository;
            _logger = logger;
        }

        public async void OnCallbackQueryAsync(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                var callbackData = e.CallbackQuery.Data;
                if (string.IsNullOrEmpty(callbackData))
                    return;

                if (!callbackData.Contains(Constants.CALLBACK_DATA_SEPARATOR))
                    throw new ArgumentException($"Prefix was not provided for callback data: {callbackData}");

                var callbackDataArr = callbackData.Split(Constants.CALLBACK_DATA_SEPARATOR, 2);
                if (callbackDataArr[0].Contains(Constants.PLAYERS_SET_CALLBACK_PREFIX))
                    await DetermineIfUserIsReadyToPlayAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId, e.CallbackQuery.From.Id, callbackData);
            }
            catch (Exception ex)
            {
                await ProcessCallbackError(e.CallbackQuery, ex);
            }
        }

        private async Task DetermineIfUserIsReadyToPlayAsync(ChatId chatId, int messageId, int userId, string callbackData)
        {
            await ClearInlineKeyboardAsync(chatId, messageId);
            await DeleteMessageAsync(chatId, messageId);

            if (IsButtonPressedAfterGame(GetDateFromCallback(callbackData)))
                return;

            var userAnswer = GetUserAnswerFromCallback(callbackData);
            var player = await _playerRepository.GetAsync(userId);
            await SheetController.GetInstance().UpdateApproveCellAsync(player.Name, GetApproveCellValue(userAnswer));

            player.IsGoingToPlay = userAnswer == Constants.YES_ANSWER;
            player.ApprovedPlayersMessageId = await SendApprovedPlayersMessage(chatId, player);

            await _playerRepository.UpdateAsync(player);
        }

        /// <summary>
        /// Sends an approved players message if it wasn't sent yet. Otherwise edits it.
        /// </summary>
        /// <param name="chatId">Player chat id</param>
        /// <param name="player">Player</param>
        /// <returns>Sent message id</returns>
        private async Task<int> SendApprovedPlayersMessage(ChatId chatId, Player player)
        {
            var approvedPlayersMessage = await SheetController.GetInstance().GetApprovedPlayersMessageAsync();

            if (player.ApprovedPlayersMessageId != 0)
            {
                try
                {
                    await _client.EditMessageTextWithTokenAsync(chatId, player.ApprovedPlayersMessageId, approvedPlayersMessage);
                    return player.ApprovedPlayersMessageId;
                }
                catch (Exception ex) // Telegram API doesn't allow to check if user deleted message
                {
                    _logger.Error(ex, $"Error on editing message for user {player.Name}");
                    return (await _client.SendTextMessageWithTokenAsync(chatId, approvedPlayersMessage)).MessageId;
                }
            }

            return (await _client.SendTextMessageWithTokenAsync(chatId, approvedPlayersMessage)).MessageId;
        }

        private DateTime GetDateFromCallback(string callbackData)
        {
            var prefix = GetCallbackValueByIndex(callbackData, 0);
            var prefixArr = prefix.Split(Constants.PLAYERS_SET_CALLBACK_PREFIX_SEPARATOR, 2);
            DateTime.TryParse(prefixArr[1], out DateTime gameDate);
            return gameDate;
        }

        private string GetUserAnswerFromCallback(string callbackData)
        {
            return GetCallbackValueByIndex(callbackData, 1);
        }

        private string GetCallbackValueByIndex(string callbackData, int index)
        {
            var callbackDataArr = callbackData.Split(Constants.CALLBACK_DATA_SEPARATOR, 2);
            if (!callbackDataArr[0].Contains(Constants.PLAYERS_SET_CALLBACK_PREFIX_SEPARATOR))
                throw new ArgumentException($"Players set separator was not provided for callback data: {callbackDataArr[0]}");
            return callbackDataArr[index];
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
            return gameDate.ToUniversalTime().Date < DateTime.UtcNow.Date;
        }

        private async Task ClearInlineKeyboardAsync(ChatId chatId, int messageId)
        {
            try
            {
                var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                await _client.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { new InlineKeyboardButton[0] }, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on clearing inline keyboard");
            }
        }

        private async Task DeleteMessageAsync(ChatId chatId, int messageId)
        {
            try
            {
                await _client.DeleteMessageAsync(chatId, messageId, new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on deleting message");
            }
        }

        private async Task ProcessCallbackError(CallbackQuery callbackQuery, Exception ex)
        {
            var messageForUser = string.Empty;
            var messageForBotOwner = string.Empty;
            var userId = callbackQuery.From.Id;
            var player = !(ex is UserNotFoundException) ? await _playerRepository.GetAsync(userId) : null;

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
            await _client.SendTextMessageWithTokenAsync(chatId, messageForUser);

            if (AppSettings.NotifyOwner)
                await _client.SendTextMessageToBotOwnerAsync(messageForBotOwner);
        }
    }
}

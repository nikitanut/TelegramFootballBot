using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Controllers
{
    public class MessageController
    {
        private readonly ILogger _logger;
        private readonly TelegramBotClient _client;
        private string _approvedPlayersMessage = null;

        public MessageController(ILogger logger)
        {
            _logger = logger;
            _client = new Bot().GetBotClient();
        }

        public void Run()
        {
            _client.OnMessage += OnMessageRecievedAsync;
            _client.OnCallbackQuery += OnCallbackQueryAsync;
            _client.StartReceiving();
        }

        public void Stop()
        {
            _client.OnMessage -= OnMessageRecievedAsync;
            _client.StopReceiving();
        }

        private async void OnMessageRecievedAsync(object sender, MessageEventArgs e)
        {
            var command = Bot.Commands.FirstOrDefault(c => c.StartsWith(e.Message));
            if (command == null)
                return;

            var playerName = string.Empty;
            try
            {
                playerName = await GetPlayerNameAsync(e.Message.From.Id);
                await command.Execute(e.Message, _client);

                if (string.IsNullOrEmpty(playerName))
                    playerName = await GetPlayerNameAsync(e.Message.From.Id);

                _logger.Information($"Command {e.Message.Text} processed for user {playerName}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on processing {e.Message.Text} command for user {playerName}");
                await SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
                await _client.SendErrorMessageToUser(e.Message.Chat.Id, playerName);                
            }
        }

        public async Task SendQuestionToAllUsersAsync()
        {
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();

            var gameDate = Scheduler.GetGameDateMoscowTime(DateTime.UtcNow.ToMoscowTime());
            var markup = MarkupHelper.GetUserDeterminationMarkup(gameDate);

            foreach (var player in await Bot.GetPlayersAsync())
            {
                var message = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";
                var request = _client.SendTextMessageWithTokenAsync(player.ChatId, message, markup);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ProcessRequests(requests, playersRequestsIds);
        }
        
        public async Task UpdateTotalPlayersMessagesAsync()
        {
            var approvedPlayersMessage = await SheetController.GetInstance().GetApprovedPlayersMessageAsync();
            if (approvedPlayersMessage == _approvedPlayersMessage)
                return;

            _approvedPlayersMessage = approvedPlayersMessage;
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();

            foreach (var player in (await Bot.GetPlayersAsync()).Where(p => p.ApprovedPlayersMessageId != 0))
            {
                var request = _client.EditMessageTextWithTokenAsync(player.ChatId, player.ApprovedPlayersMessageId, approvedPlayersMessage);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ProcessRequests(requests, playersRequestsIds);
        }

        public async Task SendTextMessageToBotOwnerAsync(string text)
        {
            if (AppSettings.NotifyOwner)
                await _client.SendTextMessageToBotOwnerAsync(text);
        }
        
        private async Task ProcessRequests(List<Task<Message>> requests, Dictionary<int, Player> playersRequestsIds)
        {
            while (requests.Count > 0)
            {
                var response = await Task.WhenAny(requests);
                requests.Remove(response);

                if (response.IsFaulted || response.IsCanceled)
                {                    
                    var playerName = playersRequestsIds.First(r => r.Key == response.Id).Value.Name;
                    var errorMessage = response.IsFaulted ? response.Exception.Message : $"Тайм-аут {Constants.ASYNC_OPERATION_TIMEOUT} мс";
                    _logger.Error($"Error for user {playerName}: {errorMessage}");               
                }
            }
        }

        private async void OnCallbackQueryAsync(object sender, CallbackQueryEventArgs e)
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

            var callbackDataArr = callbackData.Split(Constants.CALLBACK_DATA_SEPARATOR, 2);
            if (!callbackDataArr[0].Contains(Constants.PLAYERS_SET_CALLBACK_PREFIX_SEPARATOR))
                throw new ArgumentException($"Players set separator was not provided for callback data: {callbackDataArr[0]}");

            var prefixArr = callbackDataArr[0].Split(Constants.PLAYERS_SET_CALLBACK_PREFIX_SEPARATOR, 2);
            DateTime.TryParse(prefixArr[1], out DateTime gameDate);

            if (IsButtonPressedAfterGame(gameDate))
            {
                _logger.Information($"Button is pressed after game by user {userId}. Date of game: {gameDate}. Now: {DateTime.UtcNow.ToMoscowTime()}");
                return;
            }

            var userAnswer = callbackDataArr[1];
            var player = await Bot.GetPlayerAsync(userId);
            player.IsGoingToPlay = userAnswer == Constants.YES_ANSWER;

            await SheetController.GetInstance().UpdateApproveCellAsync(player.Name, GetApproveCellValue(userAnswer));

            player.ApprovedPlayersMessageId = await SendApprovedPlayersMessageAsync(chatId);
            await Bot.UpdatePlayerAsync(player);
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

        private async Task<int> SendApprovedPlayersMessageAsync(ChatId chatId)
        {
            var approvedPlayersMessage = await SheetController.GetInstance().GetApprovedPlayersMessageAsync();
            var messageSent = await _client.SendTextMessageWithTokenAsync(chatId, approvedPlayersMessage);
            return messageSent.MessageId;
        }

        private async Task ClearInlineKeyboardAsync(ChatId chatId, int messageId)
        {
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await _client.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { new InlineKeyboardButton[0] }, cancellationToken: cancellationToken);
        }

        private async Task DeleteMessageAsync(ChatId chatId, int messageId)
        {
            try { await _client.DeleteMessageAsync(chatId, messageId, new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token); }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on deleting message: ");
            }
        }

        private async Task NotifyAboutError(ChatId chatId, string messageForUser, string messageForBotOwner)
        {
            await _client.SendTextMessageWithTokenAsync(chatId, messageForUser);
            await SendTextMessageToBotOwnerAsync(messageForBotOwner);
        }

        private async Task ProcessCallbackError(CallbackQuery callbackQuery, Exception ex)
        {
            var messageForUser = string.Empty;
            var messageForBotOwner = string.Empty;
            var userId = callbackQuery.From.Id;
            var player = !(ex is UserNotFoundException)
                ? await Bot.GetPlayerAsync(userId)
                : null;

            if (ex is UserNotFoundException)
            {
                _logger.Error($"User with id {userId} not found. Name: {callbackQuery.From.FirstName} {callbackQuery.From.LastName}");
                messageForUser = "Вы не зарегистрированы. Введите команду /reg *Фамилия* *Имя*.";
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

        private async Task<string> GetPlayerNameAsync(int userId)
        {
            try
            {
                return (await Bot.GetPlayerAsync(userId)).Name;
            }
            catch (UserNotFoundException)
            {
                return string.Empty;
            }
        }
    }
}

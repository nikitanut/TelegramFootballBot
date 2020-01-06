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
        private readonly Bot _bot;
        private readonly TelegramBotClient _client;
        private readonly SheetController _sheetController;
        private string _approvedPlayersMessage = null;

        public MessageController(ILogger logger)
        {
            _logger = logger;
            _bot = new Bot();
            _client = _bot.GetBotClient();
            _sheetController = SheetController.GetInstance();
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
            foreach (var command in Bot.Commands)
            {
                if (command.Contains(e.Message))
                {
                    try
                    {
                        await command.Execute(e.Message, _client);
                        _logger.Information($"Command {e.Message.Text} processed for user {Bot.GetPlayer(e.Message.From.Id).Name}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error on processing {e.Message.Text} command for user {Bot.GetPlayer(e.Message.From.Id).Name}");
                        await _client.SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {Bot.GetPlayer(e.Message.From.Id).Name}: {ex.Message}");
                        await _client.SendErrorMessageToUser(e.Message.Chat.Id, Bot.GetPlayer(e.Message.From.Id).Name);
                    }
                }
            }
        }

        public async Task StartPlayersSetDeterminationAsync()
        {
            var message = $"Идёшь на футбол {Scheduler.GetGameDate(DateTime.Now).ToString("dd.MM")}?";
            var markup = MarkupHelper.GetKeyBoardMarkup(Constants.PLAYERS_SET_CALLBACK_PREFIX, Constants.YES_ANSWER, Constants.NO_ANSWER);

            var playersToNotify = Bot.Players.Where(p => p.IsActive);
            var requests = new List<Task<Message>>(playersToNotify.Count());
            var playersRequestsIds = new Dictionary<int, Player>(requests.Capacity);

            foreach (var player in playersToNotify)
            {
                var request = _client.SendTextMessageWithTokenAsync(player.ChatId, message, markup);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ProcessRequests(requests, playersRequestsIds);
        }

        public async Task UpdateTotalPlayersMessagesAsync()
        {
            try
            {
                var approvedPlayersMessage = await _sheetController.GetApprovedPlayersMessageAsync();
                if (approvedPlayersMessage == _approvedPlayersMessage)
                    return;

                _approvedPlayersMessage = approvedPlayersMessage;
                var playersToShowMessage = Bot.Players.Where(p => p.IsActive && p.IsGoingToPlay && p.ApprovedPlayersMessageId != 0);
                var requests = new List<Task<Message>>(playersToShowMessage.Count());
                var playersRequestsIds = new Dictionary<int, Player>(requests.Capacity);
                
                foreach (var player in playersToShowMessage)
                {
                    var request = _client.EditMessageTextWithTokenAsync(player.ChatId, player.ApprovedPlayersMessageId, approvedPlayersMessage);
                    requests.Add(request);
                    playersRequestsIds.Add(request.Id, player);
                }

                await ProcessRequests(requests, playersRequestsIds);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages");
                await _client.SendTextMessageToBotOwnerAsync($"Ошибка при обновлении сообщений с отметившимися игроками: {ex.Message}");
            }
        }

        public async void SendTextMessageToBotOwnerAsync(string text)
        {
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
                    await _client.SendTextMessageToBotOwnerAsync($"Ошибка при рассылке для игрока {playerName}: {errorMessage}");                    
                }
            }
        }

        public async Task ClearGameAttrsAsync()
        {
            var playersToUpdate = Bot.Players.Where(p => p.IsGoingToPlay || p.ApprovedPlayersMessageId != 0);
            foreach (var player in playersToUpdate)
            {
                player.IsGoingToPlay = false;
                player.ApprovedPlayersMessageId = 0;
            }

            try { await _sheetController.ClearApproveCellsAsync(); }
            catch (Exception ex)
            {
                _logger.Error(ex, "Excel-file updating error");
                SendTextMessageToBotOwnerAsync("Ошибка при обновлении excel-файла");
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
            ClearInlineKeyboardAsync(chatId, messageId);

            var callbackDataArr = callbackData.Split(Constants.CALLBACK_DATA_SEPARATOR, 2);
            if (!callbackDataArr[0].Contains(Constants.PLAYERS_SET_CALLBACK_PREFIX_SEPARATOR))
                throw new ArgumentException($"Players set separator was not provided for callback data: {callbackDataArr[0]}");

            var prefixArr = callbackDataArr[0].Split(Constants.PLAYERS_SET_CALLBACK_PREFIX_SEPARATOR, 2);
            DateTime.TryParse(prefixArr[1], out DateTime gameDate);

            if (IsButtonPressedAfterGame(gameDate))
                return;

            var userAnswer = callbackDataArr[1];
            var newCellValue = string.Empty;

            switch (userAnswer)
            {
                case Constants.YES_ANSWER: newCellValue = "1"; break;
                case Constants.NO_ANSWER: newCellValue = "0"; break;
                default: throw new ArgumentOutOfRangeException($"userAnswer: {userAnswer}");
            }

            var player = Bot.GetPlayer(userId);
            player.IsGoingToPlay = userAnswer == Constants.YES_ANSWER;

            await _sheetController.UpdateApproveCellAsync(player.Name, newCellValue);
            await SendApprovedPlayersMessageAsync(chatId, player);
        }

        private bool IsButtonPressedAfterGame(DateTime gameDate)
        {
            return gameDate.Date < Scheduler.GetGameDate(DateTime.Now).Date;
        }

        private async Task SendApprovedPlayersMessageAsync(ChatId chatId, Player player)
        {
            var approvedPlayersMessage = await _sheetController.GetApprovedPlayersMessageAsync();
            var messageSent = await _client.SendTextMessageWithTokenAsync(chatId, approvedPlayersMessage);
            player.ApprovedPlayersMessageId = messageSent.MessageId;
        }

        private async void ClearInlineKeyboardAsync(ChatId chatId, int messageId)
        {
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await _client.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { new InlineKeyboardButton[0] }, cancellationToken: cancellationToken);
        }

        private async Task NotifyAboutError(ChatId chatId, string messageForUser, string messageForBotOwner)
        {
            await _client.SendTextMessageWithTokenAsync(chatId, messageForUser);
            await _client.SendTextMessageToBotOwnerAsync(messageForBotOwner);
        }

        private async Task ProcessCallbackError(CallbackQuery callbackQuery, Exception ex)
        {
            var messageForUser = string.Empty;
            var messageForBotOwner = string.Empty;
            var userId = callbackQuery.From.Id;
            
            if (ex is UserNotFoundException)
            {
                _logger.Error($"User with id {userId} not found. Name: {callbackQuery.From.FirstName} {callbackQuery.From.LastName}");
                messageForUser = "Вы не зарегистрированы. Введите команду /register *Фамилия* *Имя*.";
                messageForBotOwner = $"Пользователь {callbackQuery.From.FirstName} {callbackQuery.From.LastName} не найден";
            }

            if (ex is TotalsRowNotFoundExeption)
            {
                _logger.Error("\"Всего\" row not found in excel-file");
                messageForUser = "Не найдена строка \"Всего\" в excel-файле.";
                messageForBotOwner = $"Не найдена строка \"Всего\" в excel-файле. Пользователь - {Bot.GetPlayer(userId).Name}";
            }

            if (ex is OperationCanceledException)
            {
                _logger.Error($"Operation {callbackQuery.Data} cancelled for user {Bot.GetPlayer(userId).Name}.");
                messageForUser = "Не удалось обработать запрос.";
                messageForBotOwner = $"Операция обработки ответа отменена для пользователя {Bot.GetPlayer(userId).Name}";
            }

            if (ex is ArgumentOutOfRangeException)
            {
                _logger.Error($"Unexpected response for user {Bot.GetPlayer(userId).Name}: {((ArgumentOutOfRangeException)ex).ParamName}");
                messageForUser = "Непредвиденный вариант ответа.";
                messageForBotOwner = $"Непредвиденный вариант ответа для пользователя {Bot.GetPlayer(userId).Name}";
            }

            if (messageForUser == string.Empty)
            {
                _logger.Error(ex, "Unexpected error");
                messageForUser = "Непредвиденная ошибка.";
                messageForBotOwner = $"Ошибка у пользователя {Bot.GetPlayer(userId).Name}: {ex.Message}";
            }
            
            await NotifyAboutError(callbackQuery.Message.Chat.Id, messageForUser, messageForBotOwner);
        }
    }
}

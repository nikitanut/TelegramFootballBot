﻿using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class MessageController
    {
        public IPlayerRepository PlayerRepository { get; }

        private readonly ILogger _logger;        
        private readonly TelegramBotClient _client;
        private string _approvedPlayersMessage = null;

        public MessageController(IPlayerRepository playerRepository, ILogger logger)
        {
            PlayerRepository = playerRepository;
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
            _client.OnCallbackQuery -= OnCallbackQueryAsync;
            _client.StopReceiving();
        }

        private async void OnMessageRecievedAsync(object sender, MessageEventArgs e)
        {
            var command = Bot.Commands.FirstOrDefault(c => c.StartsWith(e.Message));
            if (command == null)
                return;

            try
            {                
                await command.Execute(e.Message, this);
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Information($"Command {e.Message.Text} processed for user {playerName}");
            }
            catch (Exception ex)
            {
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Error(ex, $"Error on processing {e.Message.Text} command for user {playerName}");
                await SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
                await _client.SendErrorMessageToUser(e.Message.Chat.Id, playerName);                
            }
        }

        public async Task SendMessageToAllUsersAsync(string text, IReplyMarkup replyMarkup = null)
        {
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();
            
            foreach (var player in await PlayerRepository.GetAllAsync())
            {
                var request = SendMessageAsync(player.ChatId, text, replyMarkup);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ProcessRequests(requests, playersRequestsIds);
        }
        
        public async Task SendDistributionQuestionAsync()
        {
            var gameDate = Scheduler.GetNearestGameDateMoscowTime(DateTime.UtcNow);
            var message = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";
            var markup = MarkupHelper.GetUserDeterminationMarkup(gameDate);
            await SendMessageToAllUsersAsync(message, markup);
        }

        public async Task UpdateTotalPlayersMessagesAsync()
        {
            var approvedPlayersMessage = await SheetController.GetInstance().GetApprovedPlayersMessageAsync();
            if (approvedPlayersMessage == _approvedPlayersMessage)
                return;

            _approvedPlayersMessage = approvedPlayersMessage;
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();

            foreach (var player in (await PlayerRepository.GetAllAsync()).Where(p => p.ApprovedPlayersMessageId != 0))
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
        
        public async Task<Message> SendMessageAsync(ChatId chatId, string text, IReplyMarkup replyMarkup = null)
        {
            return await _client.SendTextMessageWithTokenAsync(chatId, text, replyMarkup);
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

            if (IsButtonPressedAfterGame(GetDateFromCallback(callbackData)))
                return;

            var userAnswer = GetUserAnswerFromCallback(callbackData);
            var player = await PlayerRepository.GetAsync(userId);
            player.IsGoingToPlay = userAnswer == Constants.YES_ANSWER;

            await SheetController.GetInstance().UpdateApproveCellAsync(player.Name, GetApproveCellValue(userAnswer));

            player.ApprovedPlayersMessageId = await SendApprovedPlayersMessageAsync(chatId);
            await PlayerRepository.UpdateAsync(player);
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

        private async Task<int> SendApprovedPlayersMessageAsync(ChatId chatId)
        {
            var approvedPlayersMessage = await SheetController.GetInstance().GetApprovedPlayersMessageAsync();
            var messageSent = await SendMessageAsync(chatId, approvedPlayersMessage);
            return messageSent.MessageId;
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

        private async Task NotifyAboutError(ChatId chatId, string messageForUser, string messageForBotOwner)
        {
            await SendMessageAsync(chatId, messageForUser);
            await SendTextMessageToBotOwnerAsync(messageForBotOwner);
        }

        private async Task ProcessCallbackError(CallbackQuery callbackQuery, Exception ex)
        {
            var messageForUser = string.Empty;
            var messageForBotOwner = string.Empty;
            var userId = callbackQuery.From.Id;
            var player = !(ex is UserNotFoundException) ? await PlayerRepository.GetAsync(userId) : null;

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

        private async Task<string> GetPlayerNameAsync(int userId)
        {
            try
            {
                return (await PlayerRepository.GetAsync(userId)).Name;
            }
            catch (UserNotFoundException)
            {
                return string.Empty;
            }
        }
    }
}

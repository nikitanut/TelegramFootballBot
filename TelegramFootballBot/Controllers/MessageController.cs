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

        public async void StartPlayersSetDeterminationAsync()
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

        public async void StartUpdateTotalPlayersMessagesAsync()
        {
            try
            {
                var totalPlayers = await _sheetController.GetTotalApprovedPlayersAsync();

                var playersToShowMessage = Bot.Players.Where(p => p.IsActive && p.IsGoingToPlay && p.TotalPlayersMessageId != 0);
                var requests = new List<Task<Message>>(playersToShowMessage.Count());
                var playersRequestsIds = new Dictionary<int, Player>(requests.Capacity);
                
                foreach (var player in playersToShowMessage)
                {
                    var request = _client.EditMessageTextWithTokenAsync(player.ChatId, player.TotalPlayersMessageId, $"Идут {totalPlayers} человек");
                    requests.Add(request);
                    playersRequestsIds.Add(request.Id, player);
                }

                await ProcessRequests(requests, playersRequestsIds);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages");
                await _client.SendTextMessageToBotOwnerAsync($"Ошибка при обновлении сообщения с количеством игроков: {ex.Message}");
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

        public async void ClearGameAttrs()
        {
            var playersToUpdate = Bot.Players.Where(p => p.IsGoingToPlay || p.TotalPlayersMessageId != 0);
            foreach (var player in playersToUpdate)
            {
                player.IsGoingToPlay = false;
                player.TotalPlayersMessageId = 0;
            }

            await _sheetController.ClearApproveCellsAsync();
        }

        private async void OnCallbackQueryAsync(object sender, CallbackQueryEventArgs e)
        {            
            var chatId = e.CallbackQuery.Message.Chat.Id;
            
            try
            {
                var callbackData = e.CallbackQuery.Data;
                if (string.IsNullOrEmpty(callbackData))
                    return;

                if (!callbackData.Contains(Constants.CALLBACK_DATA_SEPARATOR))
                    throw new ArgumentException($"Prefix was not provided for callback data: {callbackData}");

                var userId = e.CallbackQuery.From.Id;
                var messageId = e.CallbackQuery.Message.MessageId;
                var callbackDataArr = callbackData.Split(Constants.CALLBACK_DATA_SEPARATOR, 2);

                switch (callbackDataArr[0])
                {
                    case Constants.PLAYERS_SET_CALLBACK_PREFIX:
                        await DetermineIfUserIsReadyToPlayAsync(chatId, messageId, userId, callbackDataArr[1]);
                        break;
                }
            }
            catch (UserNotFoundException)
            {
                _logger.Information($"User with id {e.CallbackQuery.From.Id} not found. Name: {e.CallbackQuery.From.FirstName} {e.CallbackQuery.From.LastName}");
                await _client.SendTextMessageWithTokenAsync(chatId, "Пользователь не найден. Введите команду /register *Фамилия* *Имя*.");
            }
            catch (TotalsRowNotFoundExeption)
            {
                _logger.Error("\"Всего\" row not found in excel-file");
                await _client.SendTextMessageWithTokenAsync(chatId, "Не найдена строка \"Всего\" в excel-файле. Пользователь - ");
                await _client.SendTextMessageToBotOwnerAsync("Не найдена строка \"Всего\" в excel-файле.");
            }
            catch (OperationCanceledException)
            {
                _logger.Error($"Operation {e.CallbackQuery.Data} cancelled for user {Bot.GetPlayer(e.CallbackQuery.From.Id).Name}.");
                await _client.SendTextMessageWithTokenAsync(chatId, "Не удалось обработать запрос.");
                await _client.SendTextMessageToBotOwnerAsync($"Операция обработки ответа отменена для пользователя {Bot.GetPlayer(e.CallbackQuery.From.Id).Name}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.Error($"Unexpected response for user {Bot.GetPlayer(e.CallbackQuery.From.Id).Name}: {ex.ParamName}");
                await _client.SendTextMessageWithTokenAsync(chatId, "Непредвиденный вариант ответа.");
                await _client.SendTextMessageToBotOwnerAsync($"Непредвиденный вариант ответа для пользователя {Bot.GetPlayer(e.CallbackQuery.From.Id).Name}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unexpected error");
                await _client.SendTextMessageWithTokenAsync(chatId, "Непредвиденная ошибка.");
                await _client.SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {Bot.GetPlayer(e.CallbackQuery.From.Id).Name}: {ex.Message}");
            }
        }

        private async Task DetermineIfUserIsReadyToPlayAsync(long chatId, int messageId, int userId, string userAnswer)
        {
            string newCellValue = null;

            switch (userAnswer)
            {
                case Constants.YES_ANSWER: newCellValue = "1"; break;
                case Constants.NO_ANSWER: newCellValue = "0"; break;
                default:
                    throw new ArgumentOutOfRangeException($"{Constants.PLAYERS_SET_CALLBACK_PREFIX}{Constants.CALLBACK_DATA_SEPARATOR}{userAnswer}");
            }

            ClearInlineKeyboardAsync(chatId, messageId);

            var player = Bot.GetPlayer(userId);
            await _sheetController.UpdateApproveCellAsync(player.Name, newCellValue);
            
            player.IsGoingToPlay = userAnswer == Constants.YES_ANSWER;                
            if (!player.IsGoingToPlay)
                return;

            var totalPlayers = await _sheetController.GetTotalApprovedPlayersAsync();
            var totalPlayersMessage = $"Идут {totalPlayers} человек";
            var needToCreateMessage = false;

            if (player.TotalPlayersMessageId != 0)
            {
                // If user deleted message create new
                try
                {
                    var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                    await _client.EditMessageTextAsync(chatId, player.TotalPlayersMessageId, totalPlayersMessage, cancellationToken: cancellationToken);
                }
                catch { needToCreateMessage = true; }
            }
            else needToCreateMessage = true;

            if (needToCreateMessage)
            {
                var messageSent = await _client.SendTextMessageWithTokenAsync(chatId, totalPlayersMessage);
                player.TotalPlayersMessageId = messageSent.MessageId;
            }
        }

        private async void ClearInlineKeyboardAsync(ChatId chatId, int messageId)
        {
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await _client.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { new InlineKeyboardButton[0] }, cancellationToken: cancellationToken);
        }
    }
}

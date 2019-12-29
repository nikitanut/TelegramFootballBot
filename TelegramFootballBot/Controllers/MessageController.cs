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
        private readonly Bot _bot;
        private readonly TelegramBotClient _client;
        private readonly SheetController _sheetController;

        public MessageController()
        {
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
            try { ProcessMessageAsync(e.Message); }
            catch (Exception)
            {                
                var chatId = e.Message.Chat.Id;
                try
                {
                    var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                    await _client.SendTextMessageAsync(chatId, $"Неизвестная ошибка", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, cancellationToken: cancellationToken);
                }
                catch { }
                finally
                {
                    // TODO: log
                }
            }
        }

        private async void ProcessMessageAsync(Message message)
        {
            foreach (var command in Bot.Commands)
            {
                if (command.Contains(message))
                {
                    try { await command.Execute(message, _client); }
                    catch (Exception)
                    {

                    }
                    break;
                }
            }
        }

        public async void StartPlayersSetDeterminationAsync(int daysLeftBeforeGame)
        {
            var gameDate = DateTime.Now.AddDays(daysLeftBeforeGame);
            var message = $"Идёшь на футбол {gameDate.ToString("dd.MM")}?";
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

            while (requests.Count > 0)
            {
                var response = await Task.WhenAny(requests);
                requests.Remove(response);

                if (response.IsFaulted) { }
                if (response.IsCanceled) { }
                // TODO: Log
            }           
        }
        
        public async void StartUpdateTotalPlayersMessagesAsync()
        {           
            var totalPlayers = await _sheetController.GetTotalApprovedPlayersAsync();

            var playersToShowMessage = Bot.Players.Where(p => p.IsActive && p.IsGoingToPlay && p.TotalPlayersMessageId != 0);
            var requests = new List<Task<Message>>(playersToShowMessage.Count());
            var playersRequestsIds = new Dictionary<int, Player>(requests.Capacity);

            foreach (var player in playersToShowMessage)
            {
                var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                var request = _client.EditMessageTextAsync(player.ChatId, player.TotalPlayersMessageId, $"Идут {totalPlayers} человек", cancellationToken: cancellationToken);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            while (requests.Count > 0)
            {
                var response = await Task.WhenAny(requests);
                requests.Remove(response);

                if (response.IsFaulted) { }
                if (response.IsCanceled) { }
                // TODO: Set TotalPlayersMessageId Log
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

            await FileController.UpdatePlayersAsync(Bot.Players);
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
                await _client.SendTextMessageWithTokenAsync(chatId, "Пользователь не найден. Введите команду /register *Фамилия* *Имя*.");
            }
            catch (TotalsRowNotFoundExeption)
            {
                await _client.SendTextMessageWithTokenAsync(chatId, "Не найдена строка \"Всего\" в excel-файле.");
            }
            catch (OperationCanceledException)
            {
                await _client.SendTextMessageWithTokenAsync(chatId, "Не удалось обработать запрос.");
            }
            catch (ArgumentOutOfRangeException)
            {
                await _client.SendTextMessageWithTokenAsync(chatId, "Непредвиденный вариант ответа.");
            }
            catch (Exception)
            {
                // TODO
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

            if (newCellValue != null)
            {
                ClearInlineKeyboardAsync(chatId, messageId);
                await _sheetController.UpdateApproveCellAsync(userId, newCellValue);

                var player = Bot.GetPlayer(userId);
                var isGoingToPlay = userAnswer == Constants.YES_ANSWER;
                if (player.IsGoingToPlay != isGoingToPlay)
                {
                    player.IsGoingToPlay = isGoingToPlay;
                    await FileController.UpdatePlayersAsync(Bot.Players);
                }
                
                if (!isGoingToPlay)
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
                    await Bot.UpdatePlayersAsync();
                }
            }
        }

        private async void ClearInlineKeyboardAsync(ChatId chatId, int messageId)
        {
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await _client.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { new InlineKeyboardButton[0] }, cancellationToken: cancellationToken);
        }
    }
}

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
        private const string PLAYERS_SET_CALLBACK_PREFIX = "PlayersSetDetermination";

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
                    await command.Execute(message, _client);
                    break;
                }
            }
        }

        public async void StartPlayersSetDeterminationAsync(int daysLeftBeforeGame)
        {
            var gameDate = DateTime.Now.AddDays(daysLeftBeforeGame);
            var message = $"Идёшь на футбол {gameDate.ToString("dd.MM")}?";
            var markup = MarkupHelper.GetKeyBoardMarkup(PLAYERS_SET_CALLBACK_PREFIX, "Да", "Нет");

            var playersToNotify = Bot.Players.Where(p => p.IsActive);
            var requests = new List<Task<Message>>(playersToNotify.Count());
            var playersRequestsIds = new Dictionary<int, Player>(requests.Capacity);

            foreach (var player in playersToNotify)
            {
                var request = _client.SendTextMessageAsync(player.ChatId, message, replyMarkup: markup, cancellationToken: new CancellationTokenSource(5000).Token);
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
            var cts = new CancellationTokenSource(5000);
            var totalPlayers = await _sheetController.GetTotalApprovedPlayersAsync();

            var playersToShowMessage = Bot.Players.Where(p => p.TotalPlayersMessageId != 0);
            var requests = new List<Task<Message>>(playersToShowMessage.Count());

            foreach (var player in playersToShowMessage)
                requests.Add(_client.EditMessageTextAsync(player.ChatId, player.TotalPlayersMessageId, $"Идут {totalPlayers} человек", cancellationToken: cts.Token));
            
            // try/catch?
            await Task.WhenAll(requests);
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
                    case PLAYERS_SET_CALLBACK_PREFIX:
                        await DetermineIfUserIsReadyToPlayAsync(chatId, messageId, userId, callbackDataArr[1]);
                        break;
                }
            }
            catch (UserNotFoundException)
            {
                var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                await _client.SendTextMessageAsync(chatId, "Пользователь не найден. Введите команду /register *Фамилия* *Имя*.", cancellationToken: cancellationToken);
            }
            catch (TotalsRowNotFoundExeption)
            {
                var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                await _client.SendTextMessageAsync(chatId, "Не найдена строка \"Всего\" в excel-файле.", cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                await _client.SendTextMessageAsync(chatId, "Не удалось обработать запрос.", cancellationToken: cancellationToken);
            }
            catch (ArgumentOutOfRangeException)
            {
                var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                await _client.SendTextMessageAsync(chatId, "Непредвиденный вариант ответа.", cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
                // TODO
            }
        }

        private async Task DetermineIfUserIsReadyToPlayAsync(long chatId, int messageId, int userId, string userAnswer)
        {
            string newCellValue = null;

            switch (userAnswer.ToUpper())
            {
                case "ДА": newCellValue = "1"; break;
                case "НЕТ": newCellValue = "0"; break;
                default:
                    throw new ArgumentOutOfRangeException($"{PLAYERS_SET_CALLBACK_PREFIX}{Constants.CALLBACK_DATA_SEPARATOR}{userAnswer}");
            }

            if (newCellValue != null)
            {
                ClearInlineKeyboardAsync(chatId, messageId);
                await _sheetController.UpdateApproveCellAsync(userId, newCellValue);
                var totalPlayers = await _sheetController.GetTotalApprovedPlayersAsync();
                var player = Bot.Players.FirstOrDefault(p => p.Id == userId);

                if (player == null)
                    throw new UserNotFoundException();

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
                    var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                    var messageSent = await _client.SendTextMessageAsync(chatId, totalPlayersMessage, cancellationToken: cancellationToken);
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

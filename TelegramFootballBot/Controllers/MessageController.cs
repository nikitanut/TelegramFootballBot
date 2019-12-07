using System;
using System.Linq;
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

        public MessageController()
        {
            _bot = new Bot();
            _client = _bot.GetBotClient();            
        }

        public void Run()
        {
            _client.OnMessage += OnMessageRecieve;
            _client.OnCallbackQuery += OnCallbackQuery;
            _client.StartReceiving();
        }

        public void Stop()
        {
            _client.OnMessage -= OnMessageRecieve;
            _client.StopReceiving();
        }

        private async void OnMessageRecieve(object sender, MessageEventArgs e)
        {
            try { ProcessMessage(e.Message); }
            catch
            {
                // TODO: send notification
                var chatId = e.Message.Chat.Id;
                await _client.SendTextMessageAsync(chatId, $"Неизвестная ошибка", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
        }

        private async void ProcessMessage(Message message)
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

        public async void StartPlayersSetDetermination()
        {
            var daysLeftBeforeGame = GetDaysLeftBeforeGame();
            var gameDate = DateTime.Now.AddDays(daysLeftBeforeGame);
            var message = $"Идёшь на футбол {gameDate.ToString("dd.MM")}?";
            var markup = MarkupHelper.GetKeyBoardMarkup(PLAYERS_SET_CALLBACK_PREFIX, "Да", "Нет");

            foreach (var player in Bot.Players.Where(p => p.IsActive))
            {
                // The API will not allow bulk notifications to more than ~30 users per second
                await _client.SendTextMessageAsync(player.ChatId, message, replyMarkup: markup);
            }
        }

        private int GetDaysLeftBeforeGame()
        {
            var daysLeft = 0;
            var tempDate = DateTime.Now;

            // TODO: Change when implementation will be determined
            var dayOfWeek = tempDate.DayOfWeek != 0 ? (int)tempDate.DayOfWeek : 7;

            while (AppSettings.GameDay.Days != dayOfWeek)
            {
                daysLeft++;
                tempDate = tempDate.AddDays(1);
                dayOfWeek = tempDate.DayOfWeek != 0 ? (int)tempDate.DayOfWeek : 7;
            }

            return daysLeft;
        }

        private async void OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var callbackData = e.CallbackQuery.Data;
            if (string.IsNullOrEmpty(callbackData))
                return;
           
            if (!callbackData.Contains(Constants.CALLBACK_DATA_SEPARATOR))
                throw new ArgumentException($"Prefix was not provided for callback data: {callbackData}");

            var userId = e.CallbackQuery.From.Id;
            var callbackDataArr = callbackData.Split(Constants.CALLBACK_DATA_SEPARATOR, 2);

            if (callbackDataArr[0] == PLAYERS_SET_CALLBACK_PREFIX)
            {
                var sheetController = new SheetController();
                string newCellValue = null;

                switch (callbackDataArr[1].ToUpper())
                {
                    case "ДА": newCellValue = "1"; break;
                    case "НЕТ": newCellValue = "0"; break;
                    default:
                        throw new ArgumentOutOfRangeException($"{PLAYERS_SET_CALLBACK_PREFIX}{Constants.CALLBACK_DATA_SEPARATOR}{callbackDataArr[1]}");
                }

                if (newCellValue != null)
                {
                    ClearInlineKeyboard(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
                    await sheetController.UpdateApproveCell(userId, newCellValue);
                }
            }
        }

        private async void ClearInlineKeyboard(ChatId chatId, int messageId)
        {
            await _client.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { new InlineKeyboardButton[0] });
        }
    }
}

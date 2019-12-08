﻿using System;
using System.Collections.Generic;
using System.Linq;
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

            var playersToNotify = Bot.Players.Where(p => p.IsActive);
            var requests = new List<Task<Message>>(playersToNotify.Count());

            foreach (var player in playersToNotify)
                requests.Add(_client.SendTextMessageAsync(player.ChatId, message, replyMarkup: markup));
            
            await Task.WhenAll(requests);
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
                    var chatId = e.CallbackQuery.Message.Chat.Id;
                    var messageId = e.CallbackQuery.Message.MessageId;
                    ClearInlineKeyboard(chatId, messageId);

                    try
                    {
                        await _sheetController.UpdateApproveCell(userId, newCellValue);
                        var totalPlayers = await _sheetController.GetTotalApprovedPlayers();
                        var player = Bot.Players.FirstOrDefault(p => p.Id == userId);

                        if (player == null)
                            throw new UserNotFoundException();

                        var totalPlayersMessage = $"Идут {totalPlayers} человек";
                        var needToCreateMessage = false;

                        if (player.TotalPlayersMessageId != 0)
                        {
                            // If user deleted message create new
                            try { await _client.EditMessageTextAsync(chatId, player.TotalPlayersMessageId, totalPlayersMessage); }
                            catch { needToCreateMessage = true; }
                        }
                        else needToCreateMessage = true;

                        if (needToCreateMessage)
                        {
                            var messageSent = await _client.SendTextMessageAsync(chatId, totalPlayersMessage);
                            player.TotalPlayersMessageId = messageSent.MessageId;
                            await Bot.UpdatePlayers();
                        }
                    }
                    catch (UserNotFoundException)
                    {
                        await _client.SendTextMessageAsync(chatId, "Пользователь не найден. Введите команду /register *Фамилия* *Имя*.");
                    }
                    catch (TotalsRowNotFoundExeption)
                    {
                        await _client.SendTextMessageAsync(chatId, "Не найдена строка \"Всего\" в excel-файле.");
                    }
                }
            }
        }

        private async void ClearInlineKeyboard(ChatId chatId, int messageId)
        {
            await _client.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { new InlineKeyboardButton[0] });
        }
    }
}
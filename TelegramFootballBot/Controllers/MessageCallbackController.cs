﻿using Serilog;
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
using TelegramFootballBot.Models.CallbackQueries;

namespace TelegramFootballBot.Controllers
{
    public class MessageCallbackController
    {
        private readonly TelegramBotClient _client;
        private readonly TeamsController _teamsController;
        private readonly IPlayerRepository _playerRepository;
        private readonly ILogger _logger;

        public MessageCallbackController(TelegramBotClient client, TeamsController teamsController, IPlayerRepository playerRepository, ILogger logger)
        {
            _client = client;
            _teamsController = teamsController;
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

        private async Task DetermineIfUserIsReadyToPlayAsync(CallbackQuery callbackQuery)
        {
            var playerSetCallback = new PlayerSetCallback(callbackQuery.Data);
            await ClearInlineKeyboardAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);

            try
            {
                await _client.DeleteMessageWithTokenAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
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
            await SheetController.GetInstance().UpdateApproveCellAsync(player.Name, GetApproveCellValue(playerSetCallback.UserAnswer));

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

        private async Task DetermineIfUserLikesTeamAsync(CallbackQuery callbackQuery)
        {
            await ClearInlineKeyboardAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
            _teamsController.ProcessPollChoice(new TeamPollCallback(callbackQuery.Data));
            
            var player = await _playerRepository.GetAsync(callbackQuery.From.Id);
            player.PollMessageId = await SendTeamPollMessageAsync(callbackQuery.Message.Chat.Id);
            await _playerRepository.UpdateAsync(player);
        }

        private async Task<int> SendTeamPollMessageAsync(ChatId chatId)
        {
            var message = _teamsController.LikesMessage();
            return (await _client.SendTextMessageWithTokenAsync(chatId, message)).MessageId;
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
                await _client.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { new InlineKeyboardButton[0] }, cancellationToken: cancellationToken);
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
            await _client.SendTextMessageWithTokenAsync(chatId, messageForUser);

            if (AppSettings.NotifyOwner)
                await _client.SendTextMessageToBotOwnerAsync(messageForBotOwner);
        }
    }
}

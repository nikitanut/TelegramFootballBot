﻿using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using TelegramFootballBot.Core;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core.Models;
using TelegramFootballBot.Core.Models.CallbackQueries;
using TelegramFootballBot.Core.Services;
using UserNotFoundException = TelegramFootballBot.Core.Models.UserNotFoundException;

namespace TelegramFootballBot.App.Workers
{
    public class MessageProcessingWorker : BackgroundService
    {
        private readonly ITelegramBotClient _client;
        private readonly CommandFactory _commandFactory;
        private readonly IMessageService _messageService;
        private readonly ITeamService _teamService;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISheetService _sheetService;
        private readonly ILogger _logger;

        public MessageProcessingWorker(ITelegramBotClient client, CommandFactory commandFactory, IMessageService messageService, ITeamService teamService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger)
        {
            _client = client;
            _commandFactory = commandFactory;
            _messageService = messageService;
            _teamService = teamService;
            _playerRepository = playerRepository;
            _sheetService = sheetService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.OnMessage += OnMessageRecievedAsync;
            _client.OnCallbackQuery += OnCallbackQueryAsync;
            _client.StartReceiving(cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);

            _client.OnMessage -= OnMessageRecievedAsync;
            _client.OnCallbackQuery -= OnCallbackQueryAsync;
            _client.StopReceiving();
        }

        private async void OnMessageRecievedAsync(object sender, MessageEventArgs e)
        {
            var command = _commandFactory.Create(e.Message);
            if (command == null)
                return;

            try
            {
                await command.Execute(e.Message);
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Information($"Command {e.Message.Text} processed for user {playerName}");
            }
            catch (Exception ex)
            {
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Error(ex, $"Error on processing {e.Message.Text} command for user {playerName}");
                await _messageService.SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
                await _messageService.SendErrorMessageToUserAsync(e.Message.Chat.Id, playerName);
            }
        }

        private async void OnCallbackQueryAsync(object sender, CallbackQueryEventArgs e)
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

        private async Task<string> GetPlayerNameAsync(int userId)
        {
            try
            {
                return (await _playerRepository.GetAsync(userId)).Name;
            }
            catch (UserNotFoundException)
            {
                return string.Empty;
            }
        }

        private async Task DetermineIfUserIsReadyToPlayAsync(CallbackQuery callbackQuery)
        {
            var playerSetCallback = new PlayerSetCallback(callbackQuery.Data);
            await ClearInlineKeyboardAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);

            try
            {
                await _messageService.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
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
            await _sheetService.UpdateApproveCellAsync(player.Name, GetApproveCellValue(playerSetCallback.UserAnswer));

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
            var approvedPlayersMessage = await _sheetService.GetApprovedPlayersMessageAsync();

            if (player.ApprovedPlayersMessageId != 0)
            {
                try
                {
                    await _messageService.EditMessageTextAsync(chatId, player.ApprovedPlayersMessageId, approvedPlayersMessage);
                    return player.ApprovedPlayersMessageId;
                }
                catch (Exception ex) // Telegram API doesn't allow to check if user deleted message
                {
                    _logger.Error(ex, $"Error on editing message for user {player.Name}");
                    return (await _messageService.SendMessageAsync(chatId, approvedPlayersMessage)).MessageId;
                }
            }

            return (await _messageService.SendMessageAsync(chatId, approvedPlayersMessage)).MessageId;
        }

        private async Task DetermineIfUserLikesTeamAsync(CallbackQuery callbackQuery)
        {
            await ClearInlineKeyboardAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
            _teamService.ProcessPollChoice(new TeamPollCallback(callbackQuery.Data));

            var player = await _playerRepository.GetAsync(callbackQuery.From.Id);
            player.PollMessageId = await SendTeamPollMessageAsync(callbackQuery.Message.Chat.Id);
            await _playerRepository.UpdateAsync(player);
        }

        private async Task<int> SendTeamPollMessageAsync(ChatId chatId)
        {
            var message = _teamService.GetMessageWithLikes();
            return (await _messageService.SendMessageAsync(chatId, message)).MessageId;
        }

        private static string GetApproveCellValue(string userAnswer)
        {
            return userAnswer switch
            {
                Constants.YES_ANSWER => "1",
                Constants.NO_ANSWER => "0",
                Constants.MAYBE_ANSWER => "0.5",
                _ => throw new ArgumentOutOfRangeException($"userAnswer: {userAnswer}"),
            };
        }

        private static bool IsButtonPressedAfterGame(DateTime gameDate)
        {
            return gameDate.Date < DateTime.Now.Date;
        }

        private async Task ClearInlineKeyboardAsync(ChatId chatId, int messageId)
        {
            try
            {
                await _messageService.ClearReplyMarkupAsync(chatId, messageId);
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

            if (ex is ArgumentOutOfRangeException exception)
            {
                _logger.Error($"Unexpected response for user {player.Name}: {exception.ParamName}");
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
            await _messageService.SendMessageAsync(chatId, messageForUser);

            if (AppSettings.NotifyOwner)
                await _messageService.SendTextMessageToBotOwnerAsync(messageForBotOwner);
        }
    }
}

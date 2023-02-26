using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Core.Services;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core.Models;
using System.Linq;
using Telegram.Bot.Types;

namespace TelegramFootballBot.App.Workers
{
    public class SchedulerWorker : BackgroundService
    {
        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISheetService _sheetService;
        private readonly ILogger _logger;

        public SchedulerWorker(IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
            _sheetService = sheetService;
            _logger = logger;            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task DoWorkAsync()
        {
            var now = DateTime.UtcNow;
            if (DateHelper.IsTimeToAskPlayers(now))
                await SendQuestionToAllUsersAsync();

            if (DateHelper.GameStarted(now))
                await ClearGameDataAsync();

            await RefreshTotalPlayersMessagesAsync();
            await SetPlayersReadyToPlayAccordingToSheet();
        }

        private async Task ClearGameDataAsync()
        {
            try
            {
                await _sheetService.ClearGameCellsAsync();
                await DeletePlayersDataAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Clearing game attrs error");
                await _messageService.SendMessageToBotOwnerAsync("Ошибка при очищении полей");
            }
        }

        private async Task DeletePlayersDataAsync()
        {
            var playersToUpdate = await _playerRepository.GetAllAsync();
            foreach (var player in playersToUpdate)
            {
                player.ApprovedPlayersMessageId = 0;
                player.ApprovedPlayersMessage = string.Empty;
                player.IsGoingToPlay = false;
            };

            await _playerRepository.UpdateMultipleAsync(playersToUpdate);
        }

        private async Task RefreshTotalPlayersMessagesAsync()
        {
            try
            {
                var text = await _sheetService.BuildApprovedPlayersMessageAsync();
                var playersWithOutdatedMessage = await _playerRepository.GetPlayersWithOutdatedMessage(text);
                
                var messagesToRefresh = GetMessagesToRefresh(playersWithOutdatedMessage);
                await _messageService.EditMessagesAsync(text, messagesToRefresh);

                foreach (var player in playersWithOutdatedMessage)
                    player.ApprovedPlayersMessage = text;

                await _playerRepository.UpdateMultipleAsync(playersWithOutdatedMessage);
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(RefreshTotalPlayersMessagesAsync)}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(RefreshTotalPlayersMessagesAsync)}");
                await _messageService.SendMessageToBotOwnerAsync($"Ошибка при обновлении сообщений с отметившимися игроками: {ex.Message}");
            }
        }

        private static IEnumerable<Message> GetMessagesToRefresh(List<Player> playersWithOldMessage)
        {
            return playersWithOldMessage.Select(p => new Message
            {
                Text = p.ApprovedPlayersMessage,
                MessageId = p.ApprovedPlayersMessageId,
                Chat = new Chat { Id = p.ChatId }
            });
        }

        private async Task SendQuestionToAllUsersAsync()
        {
            try
            {
                var gameDate = DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow);
                var message = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";
                var markup = MarkupHelper.GetIfReadyToPlayQuestion(gameDate);
                var players = await _playerRepository.GetAllAsync();
                var chats = players.Select(p => (ChatId)p.ChatId);
                await _messageService.SendMessagesAsync(message, chats, markup);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on {nameof(SendQuestionToAllUsersAsync)}");
                await _messageService.SendMessageToBotOwnerAsync($"Ошибка при определении списка игроков: {ex.Message}");
            }
        }

        private async Task SetPlayersReadyToPlayAccordingToSheet()
        {
            var playersUpdate = new List<Player>();
            var playersReadyFromSheet = await _sheetService.GetPlayersReadyToPlayAsync();
            var playersRecievedMessages = await _playerRepository.GetRecievedMessageAsync();

            foreach (var player in playersRecievedMessages)
            {
                var isGoingToPlay = playersReadyFromSheet.Contains(player.Name);
                if (player.IsGoingToPlay != isGoingToPlay)
                {
                    player.IsGoingToPlay = isGoingToPlay;
                    playersUpdate.Add(player);
                }
            }

            await _playerRepository.UpdateMultipleAsync(playersUpdate);
        }
    }
}

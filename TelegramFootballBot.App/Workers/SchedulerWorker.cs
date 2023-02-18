using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Services;
using TelegramFootballBot.Data;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.App.Worker
{
    public class SchedulerWorker : BackgroundService
    {
        private readonly IMessageService _messageService;
        private readonly TeamsService _teamsService;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISheetService _sheetService;
        private readonly ILogger _logger;
        private bool _firstLaunch = true;

        public SchedulerWorker(IMessageService messageService, TeamsService teamsService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger)
        {
            _messageService = messageService;
            _teamsService = teamsService;
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
            if (_firstLaunch)
            {
                _firstLaunch = false;
                await ClearPlayersMessages();
            }

            var now = DateTime.UtcNow;
            if (DateHelper.DistributionTimeHasCome(now))
                await SendQuestionToAllUsersAsync();

            if (DateHelper.TeamsGenerationTimeHasCome(now) || _teamsService.IsActiveDisliked)
            {
                var players = await _sheetService.GetPlayersReadyToPlayAsync();
                await _teamsService.GenerateNewTeams(players);
                await SendGeneratedTeamsMessageAsync();
            }

            if (DateHelper.GameStarted(now))
            {
                await ClearGameAttrsAsync();
                _teamsService.ClearGeneratedTeams();
            }

            await UpdateTotalPlayersMessagesAsync();
            await UpdateTeamPollMessagesAsync();
            await SetPlayersReadyToPlayBySheet();
        }

        private async Task ClearGameAttrsAsync()
        {
            try
            {
                await _sheetService.ClearApproveCellsAsync();
                await ClearPlayersMessages();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Clearing game attrs error");
                await _messageService.SendTextMessageToBotOwnerAsync("Ошибка при очищении полей");
            }
        }

        private async Task ClearPlayersMessages()
        {
            var playersToUpdate = await _playerRepository.GetAllAsync();
            foreach (var player in playersToUpdate)
            {
                player.PollMessageId = 0;
                player.ApprovedPlayersMessageId = 0;
                player.IsGoingToPlay = false;
            };

            await _playerRepository.UpdateMultipleAsync(playersToUpdate);
        }

        private async Task UpdateTotalPlayersMessagesAsync()
        {
            try
            {
                await _messageService.UpdateTotalPlayersMessagesAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(UpdateTotalPlayersMessagesAsync)}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(UpdateTotalPlayersMessagesAsync)}");
                await _messageService.SendTextMessageToBotOwnerAsync($"Ошибка при обновлении сообщений с отметившимися игроками: {ex.Message}");
            }
        }

        private async Task UpdateTeamPollMessagesAsync()
        {
            try
            {
                await _messageService.UpdatePollMessagesAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(UpdateTeamPollMessagesAsync)}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(UpdateTeamPollMessagesAsync)}");
                await _messageService.SendTextMessageToBotOwnerAsync($"Ошибка при обновлении сообщений с отметившимися игроками: {ex.Message}");
            }
        }

        private async Task SendQuestionToAllUsersAsync()
        {
            try
            {
                await _messageService.SendDistributionQuestionAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on {nameof(SendQuestionToAllUsersAsync)}");
                await _messageService.SendTextMessageToBotOwnerAsync($"Ошибка при определении списка игроков: {ex.Message}");
            }
        }

        private async Task SendGeneratedTeamsMessageAsync()
        {
            try
            {
                await _messageService.SendTeamPollMessageAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on {nameof(SendGeneratedTeamsMessageAsync)}");
                await _messageService.SendTextMessageToBotOwnerAsync($"Ошибка при отправке сообщения с командами: {ex.Message}");
            }
        }

        private async Task SetPlayersReadyToPlayBySheet()
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

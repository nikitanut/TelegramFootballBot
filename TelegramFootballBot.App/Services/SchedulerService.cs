using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Data;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.App.Services
{
    public class SchedulerService : BackgroundService
    {
        private readonly MessageController _messageController;
        private readonly TeamsController _teamsController;
        private readonly IPlayerRepository _playerRepository;
        private readonly ILogger _logger;
        private bool _firstLaunch = true;

        public SchedulerService(MessageController messageController, TeamsController teamsController, IPlayerRepository playerRepository, ILogger logger)
        {
            _messageController = messageController;
            _teamsController = teamsController;
            _playerRepository = playerRepository;
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

            if (DateHelper.TeamsGenerationTimeHasCome(now) || _teamsController.IsActiveDisliked)
            {
                var players = await SheetController.GetInstance().GetPlayersReadyToPlay();
                await _teamsController.GenerateNewTeams(players);
                await SendGeneratedTeamsMessageAsync();
            }

            if (DateHelper.GameStarted(now))
            {
                await ClearGameAttrsAsync();
                _teamsController.ClearGeneratedTeams();
            }

            await UpdateTotalPlayersMessagesAsync();
            await UpdateTeamPollMessagesAsync();
            await SetPlayersReadyToPlayBySheet();
        }

        private async Task ClearGameAttrsAsync()
        {
            try
            {
                await SheetController.GetInstance().ClearApproveCellsAsync();
                await ClearPlayersMessages();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Clearing game attrs error");
                await _messageController.SendTextMessageToBotOwnerAsync("������ ��� �������� �����");
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
                await _messageController.UpdateTotalPlayersMessagesAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(UpdateTotalPlayersMessagesAsync)}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(UpdateTotalPlayersMessagesAsync)}");
                await _messageController.SendTextMessageToBotOwnerAsync($"������ ��� ���������� ��������� � ������������� ��������: {ex.Message}");
            }
        }

        private async Task UpdateTeamPollMessagesAsync()
        {
            try
            {
                await _messageController.UpdatePollMessagesAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(UpdateTeamPollMessagesAsync)}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(UpdateTeamPollMessagesAsync)}");
                await _messageController.SendTextMessageToBotOwnerAsync($"������ ��� ���������� ��������� � ������������� ��������: {ex.Message}");
            }
        }

        private async Task SendQuestionToAllUsersAsync()
        {
            try
            {
                await _messageController.SendDistributionQuestionAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on {nameof(SendQuestionToAllUsersAsync)}");
                await _messageController.SendTextMessageToBotOwnerAsync($"������ ��� ����������� ������ �������: {ex.Message}");
            }
        }

        private async Task SendGeneratedTeamsMessageAsync()
        {
            try
            {
                await _messageController.SendTeamPollMessageAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on {nameof(SendGeneratedTeamsMessageAsync)}");
                await _messageController.SendTextMessageToBotOwnerAsync($"������ ��� �������� ��������� � ���������: {ex.Message}");
            }
        }

        private async Task SetPlayersReadyToPlayBySheet()
        {
            var playersUpdate = new List<Player>();
            var playersReadyFromSheet = await SheetController.GetInstance().GetPlayersReadyToPlay();
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

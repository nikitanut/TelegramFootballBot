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

namespace TelegramFootballBot.App.Workers
{
    public class SchedulerWorker : BackgroundService
    {
        private readonly IMessageService _messageService;
        private readonly ITeamService _teamsService;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISheetService _sheetService;
        private readonly ILogger _logger;
        private bool _firstLaunch = true;

        public SchedulerWorker(IMessageService messageService, ITeamService teamsService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger)
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
                await DeletePlayersDataAsync();
            }

            var now = DateTime.UtcNow;
            if (DateHelper.IsTimeToAskPlayers(now))
                await SendQuestionToAllUsersAsync();

            if (DateHelper.IsTimeToGenerateTeams(now) || _teamsService.IsActiveDisliked)
            {
                var players = await _sheetService.GetPlayersReadyToPlayAsync();
                await _teamsService.GenerateNewTeams(players);
                await SendGeneratedTeamsMessageAsync();
            }

            if (DateHelper.GameStarted(now))
            {
                await ClearGameDataAsync();
                _teamsService.ClearGeneratedTeams();
            }

            await RefreshTotalPlayersMessagesAsync();
            await RefreshTeamPollMessagesAsync();
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
                await _messageService.SendMessageToBotOwnerAsync("������ ��� �������� �����");
            }
        }

        private async Task DeletePlayersDataAsync()
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

        private async Task RefreshTotalPlayersMessagesAsync()
        {
            try
            {
                await _messageService.RefreshTotalPlayersMessageAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(RefreshTotalPlayersMessagesAsync)}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(RefreshTotalPlayersMessagesAsync)}");
                await _messageService.SendMessageToBotOwnerAsync($"������ ��� ���������� ��������� � ������������� ��������: {ex.Message}");
            }
        }

        private async Task RefreshTeamPollMessagesAsync()
        {
            try
            {
                await _messageService.RefreshPollMessageAsync();
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(RefreshTeamPollMessagesAsync)}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(RefreshTeamPollMessagesAsync)}");
                await _messageService.SendMessageToBotOwnerAsync($"������ ��� ���������� ��������� � ������������� ��������: {ex.Message}");
            }
        }

        private async Task SendQuestionToAllUsersAsync()
        {
            try
            {
                var gameDate = DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow);
                var message = $"���� �� ������ {gameDate.ToRussianDayMonthString()}?";
                var markup = MarkupHelper.GetUserReadyToPlayQuestion(gameDate);
                await _messageService.SendMessageToAllPlayersAsync(message, markup);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on {nameof(SendQuestionToAllUsersAsync)}");
                await _messageService.SendMessageToBotOwnerAsync($"������ ��� ����������� ������ �������: {ex.Message}");
            }
        }

        private async Task SendGeneratedTeamsMessageAsync()
        {
            try
            {
                await _messageService.SendGeneratedTeamsMessageAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on {nameof(SendGeneratedTeamsMessageAsync)}");
                await _messageService.SendMessageToBotOwnerAsync($"������ ��� �������� ��������� � ���������: {ex.Message}");
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

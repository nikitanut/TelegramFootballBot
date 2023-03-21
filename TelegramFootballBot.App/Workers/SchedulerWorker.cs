using Telegram.Bot.Types;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Exceptions;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core.Models;
using TelegramFootballBot.Core.Services;
using Timer = System.Timers.Timer;

namespace TelegramFootballBot.App.Workers
{
    public class SchedulerWorker : IHostedService, IDisposable
    {
        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISheetService _sheetService;
        private readonly ILogger _logger;
        private Timer _timer = null;

        public SchedulerWorker(IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
            _sheetService = sheetService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            _timer = new Timer(interval);
            _timer.Elapsed += async (sender, e) => await DoWorkAsync();
            _timer.Start();
            await DoWorkAsync();
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _timer?.Stop();
            return Task.CompletedTask;
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
                var responses = await _messageService.EditMessagesAsync(text, messagesToRefresh);

                var errorResponses = responses.Where(r => r.Status == SendStatus.Error).ToList();
                await UpdateApprovedPlayersMessage(playersWithOutdatedMessage, errorResponses, text);

                if (errorResponses.Any())
                {
                    var message = string.Join(". ", errorResponses.Select(r => $"User error ({r.ChatId}) - {r.Message}"));
                    throw new SendMessageException(message);
                }
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

        private async Task UpdateApprovedPlayersMessage(List<Player> players, List<SendMessageResponse> errorResponses, string message)
        {
            var playersDidNotGetMessage = players.Join(errorResponses, p => p.ChatId, r => r.ChatId, (p, r) => p);
            var playersGotMessage = players.Except(playersDidNotGetMessage).ToList();

            foreach (var player in playersGotMessage)
                player.ApprovedPlayersMessage = message;

            await _playerRepository.UpdateMultipleAsync(playersGotMessage);
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
            try
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
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on {nameof(SetPlayersReadyToPlayAccordingToSheet)}");
                await _messageService.SendMessageToBotOwnerAsync($"An error occurred while setting players ready to play: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

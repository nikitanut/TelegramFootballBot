using Telegram.Bot.Types;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Exceptions;
using TelegramFootballBot.App.Helpers;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Services;
using Timer = System.Timers.Timer;

namespace TelegramFootballBot.App.Workers;

public class SchedulerWorker(IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger) : IHostedService, IDisposable
{
    private Timer? _timer = null;

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
            await sheetService.ClearGameCellsAsync();
            await DeletePlayersDataAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Clearing game attrs error");
            await messageService.SendMessageToBotOwnerAsync("Ошибка при очищении полей");
        }
    }

    private async Task DeletePlayersDataAsync()
    {
        var playersToUpdate = await playerRepository.GetAllAsync();
        foreach (var player in playersToUpdate)
        {
            player.ApprovedPlayersMessageId = 0;
            player.ApprovedPlayersMessage = string.Empty;
            player.IsGoingToPlay = false;
        };

        await playerRepository.UpdateMultipleAsync(playersToUpdate);
    }

    private async Task RefreshTotalPlayersMessagesAsync()
    {
        try
        {
            var text = await sheetService.BuildApprovedPlayersMessageAsync();
            var playersWithOutdatedMessage = await playerRepository.GetPlayersWithOutdatedMessage(text);

            var messagesToRefresh = GetMessagesToRefresh(playersWithOutdatedMessage);
            var responses = await messageService.EditMessagesAsync(text, messagesToRefresh);

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
            logger.Error($"The operation was canceled for {nameof(RefreshTotalPlayersMessagesAsync)}.");
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error on updating total players messages, {nameof(RefreshTotalPlayersMessagesAsync)}");
            await messageService.SendMessageToBotOwnerAsync($"Ошибка при обновлении сообщений с отметившимися игроками: {ex.Message}");
        }
    }

    private static IEnumerable<Message> GetMessagesToRefresh(List<Player> playersWithOldMessage)
    {
        return playersWithOldMessage.Select(p => new Message
        {
            Text = p.ApprovedPlayersMessage,
            Id = p.ApprovedPlayersMessageId,
            Chat = new Chat { Id = p.ChatId }
        });
    }

    private async Task UpdateApprovedPlayersMessage(List<Player> players, List<SendMessageResponse> errorResponses, string message)
    {
        var playersDidNotGetMessage = players.Join(errorResponses, p => p.ChatId, r => r.ChatId, (p, r) => p);
        var playersGotMessage = players.Except(playersDidNotGetMessage).ToList();

        foreach (var player in playersGotMessage)
            player.ApprovedPlayersMessage = message;

        await playerRepository.UpdateMultipleAsync(playersGotMessage);
    }

    private async Task SendQuestionToAllUsersAsync()
    {
        try
        {
            var gameDate = DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow);
            var message = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";
            var markup = MarkupHelper.GetIfReadyToPlayQuestion(gameDate);
            var players = await playerRepository.GetAllAsync();
            var chats = players.Select(p => (ChatId)p.ChatId);
            await messageService.SendMessagesAsync(message, chats, markup);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error on {nameof(SendQuestionToAllUsersAsync)}");
            await messageService.SendMessageToBotOwnerAsync($"Ошибка при определении списка игроков: {ex.Message}");
        }
    }

    private async Task SetPlayersReadyToPlayAccordingToSheet()
    {
        try
        {
            var playersUpdate = new List<Player>();
            var playersReadyFromSheet = await sheetService.GetPlayersReadyToPlayAsync();
            var playersRecievedMessages = await playerRepository.GetRecievedMessageAsync();

            foreach (var player in playersRecievedMessages)
            {
                var isGoingToPlay = playersReadyFromSheet.Contains(player.Name);
                if (player.IsGoingToPlay != isGoingToPlay)
                {
                    player.IsGoingToPlay = isGoingToPlay;
                    playersUpdate.Add(player);
                }
            }

            await playerRepository.UpdateMultipleAsync(playersUpdate);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error on {nameof(SetPlayersReadyToPlayAccordingToSheet)}");
            await messageService.SendMessageToBotOwnerAsync($"An error occurred while setting players ready to play: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}

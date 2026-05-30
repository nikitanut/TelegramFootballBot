namespace TelegramFootballBot.App.Services;

public interface IReceiverService
{
    Task ReceiveAsync(CancellationToken stoppingToken);
}

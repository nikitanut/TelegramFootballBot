namespace TelegramFootballBot.Core.Services
{
    public interface IReceiverService
    {
        Task ReceiveAsync(CancellationToken stoppingToken);
    }
}

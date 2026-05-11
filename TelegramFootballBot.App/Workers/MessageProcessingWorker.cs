using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Workers;

public class MessageProcessingWorker(IServiceProvider serviceProvider, ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var receiver = scope.ServiceProvider.GetRequiredService<IReceiverService>();
                await receiver.ReceiveAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.Error("Polling failed with exception: {Exception}", ex);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace TelegramFootballBot.App.Services;

public class ReceiverService(ITelegramBotClient botClient, IUpdateHandler updateHandler) : IReceiverService
{
    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.CallbackQuery, UpdateType.Message],
            DropPendingUpdates = true,
        };

        await botClient.ReceiveAsync(updateHandler, receiverOptions, stoppingToken);
    }
}

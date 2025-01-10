using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace TelegramFootballBot.Core.Services
{
    public class ReceiverService : IReceiverService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUpdateHandler _updateHandler;

        public ReceiverService(ITelegramBotClient botClient, IUpdateHandler updateHandler)
        {
            _botClient = botClient;
            _updateHandler = updateHandler;
        }

        public async Task ReceiveAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.CallbackQuery, UpdateType.Message },
                ThrowPendingUpdates = true,
            };

            await _botClient.ReceiveAsync(_updateHandler, receiverOptions, stoppingToken);
        }
    }
}

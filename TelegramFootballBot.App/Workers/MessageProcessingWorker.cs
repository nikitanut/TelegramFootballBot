using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.App.Workers
{
    public class MessageProcessingWorker : BackgroundService
    {
        private readonly ITelegramBotClient _client;
        private readonly MessageCallbackService _messageCallbackService;

        public MessageProcessingWorker(ITelegramBotClient client, MessageCallbackService messageCallbackService)
        {
            _client = client;
            _messageCallbackService = messageCallbackService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.OnMessage += _messageCallbackService.OnMessageRecievedAsync;
            _client.OnCallbackQuery += _messageCallbackService.OnCallbackQueryAsync;
            _client.StartReceiving(cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);

            _client.OnMessage -= _messageCallbackService.OnMessageRecievedAsync;
            _client.OnCallbackQuery -= _messageCallbackService.OnCallbackQueryAsync;
            _client.StopReceiving();
        }
    }
}

using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramFootballBot.Data;
using TelegramFootballBot.Models;
using TelegramFootballBot.Services;

namespace TelegramFootballBot.App.Workers
{
    public class MessageProcessingWorker : BackgroundService
    {
        private readonly Bot _bot;
        private readonly ITelegramBotClient _client;
        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;
        private readonly TeamsService _teamsService;
        private readonly ILogger _logger;
        private readonly MessageCallbackService _messageCallbackService;

        public MessageProcessingWorker(Bot bot, ITelegramBotClient client, IMessageService messageService, IPlayerRepository playerRepository, TeamsService teamsService, ISheetService sheetService, ILogger logger)
        {
            _bot = bot;
            _client = client;
            _messageService = messageService;
            _playerRepository = playerRepository;
            _teamsService = teamsService;
            _logger = logger;
            _messageCallbackService = new MessageCallbackService(_bot, _messageService, _teamsService, _playerRepository, sheetService, _logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.OnMessage += _messageCallbackService.OnMessageRecievedAsync;
            _client.OnCallbackQuery += _messageCallbackService.OnCallbackQueryAsync;
            _client.StartReceiving();

            await Task.Delay(Timeout.Infinite, stoppingToken);

            _client.OnMessage -= _messageCallbackService.OnMessageRecievedAsync;
            _client.OnCallbackQuery -= _messageCallbackService.OnCallbackQueryAsync;
            _client.StopReceiving();
        }
    }
}

using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using TelegramFootballBot.Data;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Services
{
    public class MessageWorker
    {
        public IPlayerRepository PlayerRepository { get; }

        private readonly ILogger _logger;
        private readonly ITelegramBotClient _client;
        private readonly Bot _bot;
        private readonly IMessageService _messageService;  
        private readonly TeamsService _teamsService;
        private readonly MessageCallbackService _messageCallbackService;
        private bool _isRunning = false;
        
        public MessageWorker(Bot bot, ITelegramBotClient client, IMessageService messageService, IPlayerRepository playerRepository, TeamsService teamsServiceSet, ILogger logger)
        {            
            PlayerRepository = playerRepository;
            _bot = bot;
            _client = client;
            _teamsService = teamsServiceSet;
            _logger = logger;
            _messageService = messageService;            
            _messageCallbackService = new MessageCallbackService(_messageService, _teamsService, PlayerRepository, _logger);
        }

        public void Run()
        {
            if (_isRunning)
                throw new ApplicationException("MessageService is already running");

            _client.OnMessage += OnMessageRecievedAsync;
            _client.OnCallbackQuery += _messageCallbackService.OnCallbackQueryAsync;
            _client.StartReceiving();
            _isRunning = true;
        }

        public void Stop()
        {
            _client.OnMessage -= OnMessageRecievedAsync;
            _client.OnCallbackQuery -= _messageCallbackService.OnCallbackQueryAsync;
            _client.StopReceiving();
            _isRunning = false;
        }

        private async void OnMessageRecievedAsync(object sender, MessageEventArgs e)
        {
            var command = _bot.Commands.FirstOrDefault(c => c.StartsWith(e.Message));
            if (command == null)
                return;

            try
            {                
                await command.Execute(e.Message);
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Information($"Command {e.Message.Text} processed for user {playerName}");
            }
            catch (Exception ex)
            {
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Error(ex, $"Error on processing {e.Message.Text} command for user {playerName}");
                await _messageService.SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
                await _messageService.SendErrorMessageToUserAsync(e.Message.Chat.Id, playerName);                
            }
        }

        private async Task<string> GetPlayerNameAsync(int userId)
        {
            try
            {
                return (await PlayerRepository.GetAsync(userId)).Name;
            }
            catch (UserNotFoundException)
            {
                return string.Empty;
            }
        }
    }
}

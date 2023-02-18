using System.Collections.Generic;
using Telegram.Bot;
using TelegramFootballBot.Data;
using TelegramFootballBot.Models.Commands;
using TelegramFootballBot.Models.Commands.AdminCommands;
using TelegramFootballBot.Services;

namespace TelegramFootballBot.Models
{
    public class Bot
    {
        public List<Command> Commands { get; private set; }

        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;

        public Bot(IMessageService messageService, IPlayerRepository playerRepository)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
            InitializeCommands();
        }

        public static TelegramBotClient CreateBotClient()
        {
            return new TelegramBotClient(AppSettings.BotToken);
        }
        
        private void InitializeCommands()
        {
            Commands = new List<Command>
            {
                new RegisterCommand(_messageService, _playerRepository),
                new UnregisterCommand(_messageService, _playerRepository),
                new GoCommand(_messageService, _playerRepository),
                new SwitchNotifierCommand(_messageService),
                new DistributeCommand(_messageService),
                new SayCommand(_messageService),
                new StatusCommand(_messageService, _playerRepository),
                new InfoCommand(_messageService),
                new StartCommand(_messageService),
                new ListCommand(_messageService, _playerRepository),
                new RateCommand(_messageService, _playerRepository)
            };
        }
    }
}

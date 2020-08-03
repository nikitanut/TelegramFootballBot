using System.Collections.Generic;
using Telegram.Bot;
using TelegramFootballBot.Models.Commands;
using TelegramFootballBot.Models.Commands.AdminCommands;

namespace TelegramFootballBot.Models
{
    public class Bot
    {
        private TelegramBotClient _botClient;

        public static List<Command> Commands { get; private set; }

        public TelegramBotClient GetBotClient()
        {
            if (_botClient != null)
                return _botClient;

            InitializeCommands();
            _botClient = new TelegramBotClient(AppSettings.BotToken);

            return _botClient;
        }
        
        private static void InitializeCommands()
        {
            Commands = new List<Command>
            {
                new RegisterCommand(),
                new UnregisterCommand(),
                new GoCommand(),
                new SwitchNotifierCommand(),
                new DistributeCommand(),
                new SayCommand(),
                new StatusCommand(),
                new InfoCommand(),
                new StartCommand(),
                new ListCommand(),
                new RateCommand()
            };
        }
    }
}

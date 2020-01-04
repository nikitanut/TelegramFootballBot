using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Models.Commands;

namespace TelegramFootballBot.Models
{
    public class Bot
    {
        private TelegramBotClient _botClient;
        private static List<Player> _players;

        public static List<Command> Commands { get; private set; }
        public static IReadOnlyCollection<Player> Players { get; private set; }

        public TelegramBotClient GetBotClient()
        {
            if (_botClient != null)
                return _botClient;

            InitializeCommands();
            _botClient = new TelegramBotClient(AppSettings.BotToken);
            _players = FileController.GetPlayers();
            Players = _players;

            return _botClient;
        }

        public static void AddNewPlayer(Player player)
        {
            if (player == null)
                return;

            _players.Add(player);
            Players = _players;
        }
        
        public static Player GetPlayer(int userId)
        {
            var player = Players.FirstOrDefault(p => p.Id == userId);
            return player ?? throw new UserNotFoundException();
        }

        private static void InitializeCommands()
        {
            Commands = new List<Command>
            {
                new RegisterCommand(),
                new UnregisterCommand(),
                new GoCommand(),
                new ListCommand()
            };
        }
    }
}

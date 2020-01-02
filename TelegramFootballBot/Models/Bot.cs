using System.Collections.Generic;
using System.Linq;
using Telegram.Bot;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Models.Commands;

namespace TelegramFootballBot.Models
{
    public class Bot
    {
        private TelegramBotClient botClient;
        public static List<Command> Commands { get; set; }
        public static List<Player> Players { get; set; }

        public TelegramBotClient GetBotClient()
        {
            if (botClient != null)
                return botClient;

            InitializeCommands();
            botClient = new TelegramBotClient(AppSettings.BotToken);
            Players = FileController.GetPlayersAsync();

            return botClient;
        }

        public static void AddNewPlayer(Player player)
        {
            if (player == null)
                return;

            Players.Add(player);
            FileController.UpdatePlayersAsync(Players);
        }

        public static void UpdatePlayers()
        {
            FileController.UpdatePlayersAsync(Players);
        }

        public static Player GetPlayer(int userId)
        {
            var player = Players.FirstOrDefault(p => p.Id == userId);
            if (player == null)
                throw new UserNotFoundException();
            return player;
        }

        private static void InitializeCommands()
        {
            Commands = new List<Command>
            {
                new RegisterCommand(),
                new UnregisterCommand()
            };
        }
    }
}

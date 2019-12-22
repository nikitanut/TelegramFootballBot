using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            Players = Task.Run(async () => await FileController.GetPlayersAsync()).Result;
            botClient = new TelegramBotClient(AppSettings.BotToken);

            return botClient;
        }

        public static async Task<bool> AddNewPlayerAsync(Player player)
        {
            if (player == null) return false;

            Players.Add(player);
            var isSerialized = await FileController.UpdatePlayersAsync(Players);
            return isSerialized;
        }

        public static async Task<bool> UpdatePlayersAsync()
        {
            return await FileController.UpdatePlayersAsync(Players);
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
                new StartCommand(),
                new HelloCommand(),
                new RegisterCommand(),
                new UnregisterCommand()
            };
        }
    }
}

using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramFootballBot.Data;
using TelegramFootballBot.Models.Commands;

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

        public static async Task AddNewPlayerAsync(Player player)
        {
            if (player == null)
                return;

            using (var db = new FootballBotDbContext())
            {
                db.Players.Add(player);
                await db.SaveChangesAsync();
            }
        }

        public static async Task UpdatePlayerAsync(Player player)
        {
            if (player == null || player.Id == default(int))
                return;

            using (var db = new FootballBotDbContext())
            {
                db.Entry(player).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public static async Task UpdatePlayersAsync(IEnumerable<Player> players)
        {
            using (var db = new FootballBotDbContext())
            {
                foreach (var player in players)
                    db.Entry(player).State = EntityState.Modified;
                
                await db.SaveChangesAsync();
            }
        }

        public static async Task DeletePlayerAsync(int userId)
        {
            var player = await GetPlayerAsync(userId);

            using (var db = new FootballBotDbContext())
            {
                db.Players.Remove(player);
                await db.SaveChangesAsync();
            }
        }

        public static async Task<Player> GetPlayerAsync(int userId)
        {
            using (var db = new FootballBotDbContext())
            {
                var player = await db.Players.FindAsync(userId);
                return player ?? throw new UserNotFoundException();
            }
        }

        public static async Task<List<Player>> GetPlayersAsync()
        {
            using (var db = new FootballBotDbContext())
            {
                return await db.Players.ToListAsync();
            }
        }

        private static void InitializeCommands()
        {
            Commands = new List<Command>
            {
                new RegisterCommand(),
                new UnregisterCommand(),
                new GoCommand(),
                new SwitchNotifierCommand()
            };
        }
    }
}

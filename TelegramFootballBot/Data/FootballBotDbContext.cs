using Microsoft.EntityFrameworkCore;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Data
{
    public class FootballBotDbContext : DbContext
    {
        private static readonly DbContextOptions<FootballBotDbContext> options = 
            new DbContextOptionsBuilder<FootballBotDbContext>().UseSqlite("Filename=./BotDb.db").Options;
        
        public FootballBotDbContext() : base(options)
        { }

        public FootballBotDbContext(DbContextOptions<FootballBotDbContext> options) : base(options) { }

        public DbSet<Player> Players { get; set; }
    }
}

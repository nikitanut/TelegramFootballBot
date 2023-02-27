using Microsoft.EntityFrameworkCore;
using TelegramFootballBot.Core.Models;

namespace TelegramFootballBot.Core.Data
{
    public class FootballBotDbContext : DbContext
    {        
        public FootballBotDbContext(DbContextOptions<FootballBotDbContext> options) : base(options) 
        { }

        public DbSet<Player> Players { get; set; }
    }
}

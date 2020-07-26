using Microsoft.EntityFrameworkCore;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Data
{
    public class FootballBotDbContext : DbContext
    {        
        public FootballBotDbContext(DbContextOptions<FootballBotDbContext> options) : base(options) 
        { }

        public DbSet<Player> Players { get; set; }
    }
}

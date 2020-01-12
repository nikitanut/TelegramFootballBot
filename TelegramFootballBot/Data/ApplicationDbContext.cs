using Microsoft.EntityFrameworkCore;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Data
{
    public class ApplicationDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=./BotDb.db");
        }

        public DbSet<Player> Players { get; set; }
    }
}

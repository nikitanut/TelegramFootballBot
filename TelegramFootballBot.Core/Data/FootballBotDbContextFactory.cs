using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TelegramFootballBot.Core.Data
{
    public class FootballBotDbContextFactory : IDesignTimeDbContextFactory<FootballBotDbContext>
    {
        public FootballBotDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FootballBotDbContext>();

            optionsBuilder.UseSqlite("Data Source=BotDb.db");

            return new FootballBotDbContext(optionsBuilder.Options);
        }
    }
}

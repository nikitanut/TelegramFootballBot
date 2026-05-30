using Microsoft.EntityFrameworkCore;
using TelegramFootballBot.App.Models;

namespace TelegramFootballBot.App.Data;

public class FootballBotDbContext(DbContextOptions<FootballBotDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players { get; set; }
}

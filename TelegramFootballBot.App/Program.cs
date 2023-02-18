using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using System.Threading.Tasks;
using TelegramFootballBot.Services;
using TelegramFootballBot.Data;
using TelegramFootballBot.Models;
using TelegramFootballBot.App.Worker;

namespace TelegramFootballBot.App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<ILogger, Logger>(s => new LoggerConfiguration()
                        .WriteTo.File("logs.txt", outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger());

                    services.AddSingleton<IPlayerRepository, PlayerRepository>(s => 
                        new PlayerRepository(new DbContextOptionsBuilder<FootballBotDbContext>().UseSqlite("Filename=./BotDb.db").Options));

                    services.AddHostedService(s => 
                    {
                        var logger = s.GetRequiredService<ILogger>();
                        var teamSet = new TeamsService(s.GetRequiredService<IPlayerRepository>());
                        var playerRepository = s.GetRequiredService<IPlayerRepository>();
                        var botClient = Bot.CreateBotClient();
                        var messageService = new MessageService(botClient, playerRepository, teamSet, logger);
                        return new SchedulerWorker(messageService, teamSet, playerRepository, logger);
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}

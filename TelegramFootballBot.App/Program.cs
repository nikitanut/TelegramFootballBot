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
using System.IO;

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

                    services.AddSingleton<IPlayerRepository>(s => 
                        new PlayerRepository(new DbContextOptionsBuilder<FootballBotDbContext>().UseSqlite("Filename=./BotDb.db").Options));

                    services.AddSingleton<ISheetService>(s =>
                    {
                        using (var credentialsFile = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                        {
                            return new SheetService(credentialsFile);
                        };
                    });

                    services.AddHostedService(s => 
                    {
                        var logger = s.GetRequiredService<ILogger>();
                        var teamSet = new TeamsService(s.GetRequiredService<IPlayerRepository>());
                        var playerRepository = s.GetRequiredService<IPlayerRepository>();
                        var sheetService = s.GetRequiredService<ISheetService>();
                        var botClient = Bot.CreateBotClient();
                        var messageService = new MessageService(botClient, playerRepository, teamSet, sheetService, logger);
                        return new SchedulerWorker(messageService, teamSet, playerRepository, sheetService, logger);
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using System.Threading.Tasks;
using TelegramFootballBot.Core.Services;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.App.Workers;
using System.IO;
using Telegram.Bot;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core;

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
                                        
                    services.AddSingleton<ITelegramBotClient>(s => new TelegramBotClient(AppSettings.BotToken));
                    services.AddSingleton<ITeamService, TeamService>();
                    services.AddSingleton<IMessageService, MessageService>();
                    services.AddSingleton<CommandFactory>();
                    services.AddSingleton<MessageCallbackService>();

                    services.AddHostedService<SchedulerWorker>();
                    services.AddHostedService<MessageProcessingWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}

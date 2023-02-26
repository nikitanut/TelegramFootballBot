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
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Polling;
using TelegramFootballBot.Core.Clients;

namespace TelegramFootballBot.App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

                    services.AddSingleton<ILogger, Logger>(s => new LoggerConfiguration()
                        .WriteTo.File("logs.txt", outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger());

                    services.AddSingleton<IPlayerRepository>(s => 
                        new PlayerRepository(new DbContextOptionsBuilder<FootballBotDbContext>().UseSqlite("Filename=./BotDb.db").Options));

                    services.AddSingleton<ISheetService>(s =>
                    {
                        using (var credentialsFile = new FileStream("sheetcredentials.json", FileMode.Open, FileAccess.Read))
                        {
                            return new SheetService(credentialsFile, configuration["googleDocSheetId"]);
                        };
                    });
                                        
                    services.AddSingleton<ITelegramBotClient>(s => new TelegramBotClient(configuration["botToken"]));
                    services.AddSingleton<IBotClient, BotClient>();
                    services.AddSingleton<IMessageService, MessageService>();
                    services.AddSingleton<CommandFactory>();
                    services.AddScoped<IUpdateHandler, UpdateHandler>();
                    services.AddScoped<IReceiverService, ReceiverService>();
                    
                    services.AddHostedService<SchedulerWorker>();
                    services.AddHostedService<MessageProcessingWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}

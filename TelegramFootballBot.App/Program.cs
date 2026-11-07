using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramFootballBot.App.Workers;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Services;
using TelegramFootballBot.App.Commands;

namespace TelegramFootballBot.App;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = new ConfigurationBuilder().AddJsonFile(AppData.File("appsettings.json")).Build();
                var logPath = AppData.File("logs.txt");

                services.AddSingleton<ILogger, Logger>(_ =>
                {
                    var logger = new LoggerConfiguration()
                        .WriteTo.Console(outputTemplate: "{Timestamp:dd.MM HH:mm:ss} {Level:u3} - {Message:lj}{NewLine}{Exception}")
                        .WriteTo.File(logPath, outputTemplate: "{Timestamp:dd.MM HH:mm:ss} {Level:u3} - {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();
                    logger.Information("Data directory: {DataDirectory}", AppData.DataDirectory ?? Directory.GetCurrentDirectory());
                    return logger;
                });

                var databasePath = AppData.DatabasePath();
                services.AddSingleton<IPlayerRepository>(_ =>
                    new PlayerRepository(new DbContextOptionsBuilder<FootballBotDbContext>().UseSqlite($"Data Source={databasePath}").Options));

                var credentialsPath = AppData.File("sheetcredentials.json");
                services.AddSingleton<ISheetService>(s =>
                {
                    using (var credentialsFile = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
                    {
                        return new SheetService(credentialsFile, configuration["googleDocSheetId"] ?? throw new ApplicationException("googleDocSheetId is empty"));
                    };
                });

                services.AddSingleton<ITelegramBotClient>(s => new TelegramBotClient(configuration["botToken"] ?? throw new ApplicationException("botToken is empty")));
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

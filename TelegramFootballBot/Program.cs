using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using TelegramFootballBot.Services;
using TelegramFootballBot.Data;
using TelegramFootballBot.Models;
using System.IO;
using Telegram.Bot;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.File("logs.txt", outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            
            var botClient = new TelegramBotClient(AppSettings.BotToken);
            
            try
            {
                logger.Information("Bot started");

                var playerRepository = new PlayerRepository(new DbContextOptionsBuilder<FootballBotDbContext>().UseSqlite("Filename=./BotDb.db").Options);
                var teamsService = new TeamService(playerRepository);
                
                SheetService sheetService;
                using (var credentialsFile = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    sheetService = new SheetService(credentialsFile);
                };

                var messageService = new MessageService(botClient, playerRepository, teamsService, sheetService, logger);                
                var commandFactory = new CommandFactory(messageService, playerRepository, sheetService);
                var scheduler = new Scheduler(messageService, teamsService, playerRepository, sheetService, logger);
                var messageCallbackService = new MessageCallbackService(commandFactory, messageService, teamsService, playerRepository, sheetService, logger);
                var messageWorker = new MessageWorker(botClient, messageCallbackService);

                messageWorker.Run();
                scheduler.Run();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "FATAL ERROR");
                throw;
            }
        }
    }
}

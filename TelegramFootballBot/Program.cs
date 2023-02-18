using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using TelegramFootballBot.Services;
using TelegramFootballBot.Data;
using TelegramFootballBot.Models;
using System.IO;

namespace TelegramFootballBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.File("logs.txt", outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            
            var botClient = Bot.CreateBotClient();
            
            try
            {
                logger.Information("Bot started");

                var playerRepository = new PlayerRepository(new DbContextOptionsBuilder<FootballBotDbContext>().UseSqlite("Filename=./BotDb.db").Options);
                var teamService = new TeamsService(playerRepository);
                
                SheetService sheetService;
                using (var credentialsFile = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    sheetService = new SheetService(credentialsFile);
                };

                var messageService = new MessageService(botClient, playerRepository, teamService, sheetService, logger);                
                var bot = new Bot(messageService, playerRepository, sheetService);
                var scheduler = new Scheduler(messageService, teamService, playerRepository, sheetService, logger);
                var messageWorker = new MessageWorker(bot, botClient, messageService, playerRepository, teamService, sheetService, logger);

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

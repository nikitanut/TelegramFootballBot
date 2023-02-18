using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Threading;
using TelegramFootballBot.Services;
using TelegramFootballBot.Data;
using TelegramFootballBot.Models;

namespace TelegramFootballBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.File("logs.txt", outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                logger.Information("Bot started");

                var playerRepository = new PlayerRepository(new DbContextOptionsBuilder<FootballBotDbContext>().UseSqlite("Filename=./BotDb.db").Options);
                var teamSet = new TeamsService(playerRepository);
                var messageService = new MessageService(playerRepository, teamSet, logger);
                var scheduler = new Scheduler(messageService, teamSet, playerRepository, logger);

                messageService.Run();
                scheduler.Run();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "FATAL ERROR");
                try { new MessageService(null, null, logger).SendTextMessageToBotOwnerAsync($"Ошибка приложения: {ex.Message}").Wait(); }
                catch { }
                throw;
            }
        }
    }
}

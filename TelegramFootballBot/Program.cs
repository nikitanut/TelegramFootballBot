﻿using Serilog;
using System;
using System.Threading;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Data;

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

                var playerRepository = new PlayerRepository();
                var messageController = new MessageController(playerRepository, logger);
                var scheduler = new Scheduler(messageController, playerRepository, logger);

                messageController.Run();
                scheduler.Run();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "FATAL ERROR");
                try { new MessageController(null, logger).SendTextMessageToBotOwnerAsync($"Ошибка приложения: {ex.Message}").Wait(); }
                catch { }
                throw;
            }
        }
    }
}

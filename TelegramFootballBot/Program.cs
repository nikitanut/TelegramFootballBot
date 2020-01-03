using Serilog;
using System;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var fatalErrorsCount = 0;
            var logger = new LoggerConfiguration()
                .WriteTo.File("logs.txt", outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            while (fatalErrorsCount < 5)
            {
                try
                {
                    logger.Information("Bot started");
                    var messageController = new MessageController(logger);
                    var scheduler = new Scheduler(messageController, logger);

                    messageController.Run();
                    scheduler.Run();

                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    fatalErrorsCount++;
                    logger.Fatal(ex, "FATAL ERROR");
                    new MessageController(logger).SendTextMessageToBotOwnerAsync($"Ошибка приложения: {ex.Message}");
                }
            }
        }
    }
}

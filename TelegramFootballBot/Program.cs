using System;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var messageController = new MessageController();
            var scheduler = new Scheduler(messageController);
            messageController.Run();
            scheduler.Run();

            Console.ReadLine();
        }
    }
}

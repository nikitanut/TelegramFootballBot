using System;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var messageProcessor = new MessageController();
            var scheduler = new Scheduler(messageProcessor);
            messageProcessor.StartPlayersSetDeterminationAsync(3);
            messageProcessor.Run();
            //scheduler.Run();

            Console.ReadLine();
        }
    }
}

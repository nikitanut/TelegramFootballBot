using System;
using TelegramFootballBot.Models;
using TelegramFootballBot.Models.Processors;

namespace TelegramFootballBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var messageProcessor = new MessageProcessor();
            var scheduler = new Scheduler(messageProcessor);
            messageProcessor.StartPlayersSetDetermination();
            messageProcessor.Run();
            //scheduler.Run();

            Console.ReadLine();
        }
    }
}

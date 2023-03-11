using System;
using TelegramFootballBot.Queue.Models;

namespace TelegramFootballBot.Queue
{
    public interface IConsumer : IDisposable
    {
        Message Consume();
    }
}

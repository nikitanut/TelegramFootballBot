using System;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Queue.Models;

namespace TelegramFootballBot.Queue
{
    public interface IProducer : IDisposable
    {
        Task ProduceAsync(Message request, CancellationToken cancellationToken = default);
    }
}

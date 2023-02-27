using System.Threading;
using System.Threading.Tasks;

namespace TelegramFootballBot.Core.Services
{
    public interface IReceiverService
    {
        Task ReceiveAsync(CancellationToken stoppingToken);
    }
}

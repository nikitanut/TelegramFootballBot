using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramFootballBot.Core.Models;

namespace TelegramFootballBot.Core.Data
{
    public interface IPlayerRepository
    {
        Task<Player> GetAsync(long id);

        Task AddAsync(Player player);

        Task UpdateAsync(Player player);

        Task UpdateMultipleAsync(IEnumerable<Player> players);

        Task RemoveAsync(long id);

        Task<List<Player>> GetAllAsync();

        Task<List<Player>> GetRecievedMessageAsync();
    }
}

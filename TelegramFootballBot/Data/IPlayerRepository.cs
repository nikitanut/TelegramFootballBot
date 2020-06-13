using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Data
{
    public interface IPlayerRepository
    {
        Task<Player> GetAsync(int id);
        Task AddAsync(Player player);
        Task UpdateAsync(Player player);
        Task UpdateMultipleAsync(IEnumerable<Player> players);
        Task RemoveAsync(int id);
        Task<IEnumerable<Player>> GetAllAsync();
        Task<IEnumerable<Player>> GetReadyToPlayAsync();
    }
}

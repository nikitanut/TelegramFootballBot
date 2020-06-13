using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Data
{
    public class PlayerRepository : IPlayerRepository
    {
        public async Task AddAsync(Player player)
        {
            if (player == null)
                return;

            using (var db = new FootballBotDbContext())
            {
                db.Players.Add(player);
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<Player>> GetAllAsync()
        {
            using (var db = new FootballBotDbContext())
            {
                return await db.Players.ToListAsync();
            }
        }

        public async Task<Player> GetAsync(int id)
        {
            using (var db = new FootballBotDbContext())
            {
                var player = await db.Players.FindAsync(id);
                return player ?? throw new UserNotFoundException();
            }
        }

        public async Task RemoveAsync(int id)
        {
            var player = await GetAsync(id);

            using (var db = new FootballBotDbContext())
            {
                db.Players.Remove(player);
                await db.SaveChangesAsync();
            }
        }

        public async Task UpdateAsync(Player player)
        {
            if (player == null || player.Id == default(int))
                return;

            using (var db = new FootballBotDbContext())
            {
                db.Entry(player).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        public async Task UpdateMultipleAsync(IEnumerable<Player> players)
        {
            using (var db = new FootballBotDbContext())
            {
                foreach (var player in players)
                    db.Entry(player).State = EntityState.Modified;

                await db.SaveChangesAsync();
            }
        }

        public async Task<List<Player>> GetReadyToPlayAsync()
        {
            using (var db = new FootballBotDbContext())
            {
                return (await db.Players.ToListAsync()).Where(p => p.ApprovedPlayersMessageId != 0).ToList();
            }
        }
    }
}

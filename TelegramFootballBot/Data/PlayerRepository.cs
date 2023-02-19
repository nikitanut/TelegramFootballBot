using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramFootballBot.Core.Models;

namespace TelegramFootballBot.Core.Data
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly DbContextOptions<FootballBotDbContext> _options;

        public PlayerRepository(DbContextOptions<FootballBotDbContext> options)
        {
            _options = options;
            using var db = new FootballBotDbContext(_options);
            db.Database.EnsureCreated();
        }

        public async Task AddAsync(Player player)
        {
            if (player == null)
                return;

            using var db = new FootballBotDbContext(_options);
            db.Players.Add(player);
            await db.SaveChangesAsync();
        }

        public async Task<List<Player>> GetAllAsync()
        {
            using var db = new FootballBotDbContext(_options);
            return await db.Players.ToListAsync();
        }

        public async Task<Player> GetAsync(int id)
        {
            using var db = new FootballBotDbContext(_options);
            return await GetAsync(id, db);
        }

        private static async Task<Player> GetAsync(int id, FootballBotDbContext context)
        {
            var player = await context.Players.FindAsync(id);
            return player ?? throw new UserNotFoundException();
        }

        public async Task<Player> GetAsync(string name)
        {
            using var db = new FootballBotDbContext(_options);
            var player = await db.Players.FirstOrDefaultAsync(p => p.Name.Equals(name, System.StringComparison.InvariantCultureIgnoreCase));
            return player ?? throw new UserNotFoundException();
        }

        public async Task RemoveAsync(int id)
        {
            using var db = new FootballBotDbContext(_options);
            var player = await GetAsync(id, db);
            db.Players.Remove(player);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Player player)
        {
            if (player == null || player.Id == default)
                return;

            using var db = new FootballBotDbContext(_options);
            db.Entry(player).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public async Task UpdateMultipleAsync(IEnumerable<Player> players)
        {
            if (!players.Any()) return;

            using var db = new FootballBotDbContext(_options);

            foreach (var player in players)
                db.Entry(player).State = EntityState.Modified;

            await db.SaveChangesAsync();
        }

        public async Task<List<Player>> GetRecievedMessageAsync()
        {
            using var db = new FootballBotDbContext(_options);
            return (await db.Players.ToListAsync()).Where(p => p.ApprovedPlayersMessageId != 0).ToList();
        }

        public async Task<List<Player>> GetReadyToPlayAsync()
        {
            using var db = new FootballBotDbContext(_options);
            return (await db.Players.ToListAsync()).Where(p => p.IsGoingToPlay == true).ToList();
        }
    }
}

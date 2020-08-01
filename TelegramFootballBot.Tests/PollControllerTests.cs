using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Linq;
using TelegramFootballBot.Data;
using TelegramFootballBot.Models;
using TelegramFootballBot.Models.CallbackQueries;
using Xunit;

namespace TelegramFootballBot.Tests
{
    public class PollControllerTests
    {
        private readonly IPlayerRepository _playerRepository;

        public PollControllerTests()
        {
            var dbOptions = new DbContextOptionsBuilder<FootballBotDbContext>()
                              .UseSqlite(CreateInMemoryDatabase())
                              .Options;

            _playerRepository = new PlayerRepository(dbOptions);
        }
        private static DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();
            return connection;
        }

        [Fact]
        public void ProcessPollChoise_ReturnsCorrectAmountOfLikes()
        {
            var teamSet = new TeamsController(_playerRepository);

            var pollId = teamSet.GetActivePollId();
            var callBacks = new[]
            {
                new TeamPollCallback($"TeamPoll|{pollId}_Да"),
                new TeamPollCallback($"TeamPoll|{pollId}_Нет"),
                new TeamPollCallback($"TeamPoll|{pollId}_Да"),
                new TeamPollCallback($"TeamPoll|{pollId}_Да")
            };

            foreach (var callBack in callBacks)
                teamSet.ProcessPollChoice(callBack);

            Assert.Equal(3, teamSet.ActiveLikes);
            Assert.Equal(1, teamSet.ActiveDislikes);
        }
    }
}

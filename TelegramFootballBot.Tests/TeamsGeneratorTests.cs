using System.Collections.Generic;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Models;
using Xunit;

namespace TelegramFootballBot.Tests
{
    public class TeamsGeneratorTests
    {
        [Fact]
        public void GenerateTeams_GeneratesCorrectTeams()
        {
            var id = 1;
            var players = new List<Player>
            {
                new Player($"{id++}") { Rating = 84 },
                new Player($"{id++}") { Rating = 90 },
                new Player($"{id++}") { Rating = 88 },
                new Player($"{id++}") { Rating = 86 },
                new Player($"{id++}") { Rating = 82 },
                new Player($"{id++}") { Rating = 75 },
                new Player($"{id++}") { Rating = 60 },
                new Player($"{id++}") { Rating = 64 },
                new Player($"{id++}") { Rating = 73 },
                new Player($"{id++}") { Rating = 75 },
                new Player($"{id++}") { Rating = 76 },
                new Player($"{id++}") { Rating = 71 }
            };
            var teams = TeamsGenerator.Generate(players);

            Assert.Equal(77, teams[0].AverageRating);
            Assert.Equal(77, teams[1].AverageRating);
        }
    }
}

using System;
using System.Linq;
using TelegramFootballBot.Controllers;
using Xunit;

namespace TelegramFootballBot.Tests
{
    public class TeamsGeneratorTests
    {
        [Fact]
        public void GenerateTeams_GeneratesCorrectTeams()
        {            
            var teams = TeamsGenerator.Generate(TestPlayerSet.Get());
            var minDelta = teams.Min(v => Math.Abs(v[0].AverageRating - v[1].AverageRating));
            var optimalTeam = teams.First(v => minDelta == Math.Abs(v[0].AverageRating - v[1].AverageRating));

            Assert.Equal(77.6, optimalTeam[0].AverageRating);
            Assert.Equal(77.8, optimalTeam[1].AverageRating);
        }
    }
}

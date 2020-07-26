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

            Assert.Equal(77, teams[0][0].AverageRating);
            Assert.Equal(77, teams[0][1].AverageRating);
        }
    }
}

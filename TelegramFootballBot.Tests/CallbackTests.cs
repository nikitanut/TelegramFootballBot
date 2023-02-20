using System;
using TelegramFootballBot.Core.Models.CallbackQueries;
using Xunit;

namespace TelegramFootballBot.Tests
{
    public class CallbackTests
    {
        [Fact]
        public void GetGameStartCallbackText_ReturnsCorrectText()
        {
            var gameDate = new DateTime(2020, 07, 25);
            var text = PlayerSetCallback.BuildCallbackPrefix(gameDate);

            Assert.Equal("PlayersSetDetermination|2020-07-25", text);
        }

        [Fact]
        public void GetCallbackGameDate_ReturnsCorrectDate()
        {
            var callBack = new PlayerSetCallback("PlayersSetDetermination|2020-07-25_Да");

            Assert.Equal(new DateTime(2020, 07, 25), callBack.GameDate);
        }

        [Fact]
        public void ToCallbackText_ReturnsCorrectText()
        {
            var playerSetCallbackText = Callback.ToCallbackText("PlayersSetDetermination|2020-07-25", "Да");
            var teamPollText = Callback.ToCallbackText("TeamPoll|84d7e364-a716-4d30-916a-478d88cd1a87", "Нет");

            Assert.Equal("PlayersSetDetermination|2020-07-25_Да", playerSetCallbackText);
            Assert.Equal("TeamPoll|84d7e364-a716-4d30-916a-478d88cd1a87_Нет", teamPollText);
        }

        [Fact]
        public void GetCallbackName_ReturnsCorrectName()
        {
            var playerSetCallbackName = Callback.GetCallbackName("PlayersSetDetermination|2020-07-25_Да");
            var teamPollCallbackName = Callback.GetCallbackName("TeamPoll|84d7e364-a716-4d30-916a-478d88cd1a87_Нет");

            Assert.Equal("PlayersSetDetermination", playerSetCallbackName);
            Assert.Equal("TeamPoll", teamPollCallbackName);
        }
    }
}

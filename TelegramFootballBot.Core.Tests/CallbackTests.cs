using Microsoft.VisualStudio.TestTools.UnitTesting;
using TelegramFootballBot.Core.Models.CallbackQueries;

namespace TelegramFootballBot.Core.Tests
{
    [TestClass]
    public class CallbackTests
    {
        [TestMethod]
        public void GetGameStartCallbackText_ReturnsCorrectText()
        {
            // Arrange
            var gameDate = new DateTime(2020, 07, 25);

            // Act
            var text = PlayerSetCallback.BuildCallbackPrefix(gameDate);

            // Assert
            Assert.AreEqual("PlayersSetDetermination|2020-07-25", text);
        }

        [TestMethod]
        public void GetCallbackGameDate_ReturnsCorrectDate()
        {
            // Arrange
            var callBack = new PlayerSetCallback("PlayersSetDetermination|2020-07-25_Да");

            // Act
            var gameDate = callBack.GameDate;

            // Assert
            Assert.AreEqual(new DateTime(2020, 07, 25), gameDate);
        }

        [TestMethod]
        public void ToCallbackText_ReturnsCorrectText()
        {
            // Arrange
            var playerSetPrefix = "PlayersSetDetermination|2020-07-25";
            var playerSetText = "Да";
            var teamPollPrefix = "TeamPoll|84d7e364-a716-4d30-916a-478d88cd1a87";
            var teamPollText = "Нет";

            // Act
            var playerSetCallbackText = Callback.ToCallbackText(playerSetPrefix, playerSetText);
            var teamPollCallbackText = Callback.ToCallbackText(teamPollPrefix, teamPollText);

            // Assert
            Assert.AreEqual("PlayersSetDetermination|2020-07-25_Да", playerSetCallbackText);
            Assert.AreEqual("TeamPoll|84d7e364-a716-4d30-916a-478d88cd1a87_Нет", teamPollCallbackText);
        }

        [TestMethod]
        public void GetCallbackName_ReturnsCorrectName()
        {
            // Arrange
            var playerSetCallbackData = "PlayersSetDetermination|2020-07-25_Да";
            var teamPollCallbackData = "TeamPoll|84d7e364-a716-4d30-916a-478d88cd1a87_Нет";

            // Act
            var playerSetCallbackName = Callback.GetCallbackName(playerSetCallbackData);
            var teamPollCallbackName = Callback.GetCallbackName(teamPollCallbackData);

            // Assert
            Assert.AreEqual("PlayersSetDetermination", playerSetCallbackName);
            Assert.AreEqual("TeamPoll", teamPollCallbackName);
        }
    }
}

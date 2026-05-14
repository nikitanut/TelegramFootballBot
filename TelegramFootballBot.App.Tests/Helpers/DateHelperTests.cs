using TelegramFootballBot.App.Helpers;

namespace TelegramFootballBot.App.Tests.Helpers;

[TestClass]
public class DateHelperTests
{
    [TestMethod]
    public void ToMoscowTime_ShouldAddMoscowOffset()
    {
        // Arrange
        var utcDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = utcDate.ToMoscowTime();

        // Assert
        Assert.AreEqual(new DateTime(2025, 1, 1, 15, 0, 0), result);
    }

    [TestMethod]
    public void ToRussianDayMonthString_ShouldReturnRussianDate()
    {
        // Arrange
        var date = new DateTime(2025, 5, 10);

        // Act
        var result = date.ToRussianDayMonthString();

        // Assert
        Assert.AreEqual("10 мая", result);
    }

    [TestMethod]
    public void GetNearestGameDateMoscowTime_ShouldReturnCorrectGameDate()
    {
        // Arrange
        var currentDate = new DateTime(2025, 5, 5, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var result = DateHelper.GetNearestGameDateMoscowTime(currentDate);

        // Assert
        Assert.AreEqual((int)AppSettings.GameDay.Days, (int)result.DayOfWeek == 0 ? 7 : (int)result.DayOfWeek);
        Assert.AreEqual(AppSettings.GameDay.Hours, result.Hour);
        Assert.AreEqual(AppSettings.GameDay.Minutes, result.Minute);
    }

    [TestMethod]
    public void GetNearestDistributionDateMoscowTime_ShouldReturnCorrectDistributionDate()
    {
        // Arrange
        var currentDate = new DateTime(2025, 5, 5, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var result = DateHelper.GetNearestDistributionDateMoscowTime(currentDate);

        // Assert
        Assert.AreEqual((int)AppSettings.DistributionTime.Days, (int)result.DayOfWeek == 0 ? 7 : (int)result.DayOfWeek);
        Assert.AreEqual(AppSettings.DistributionTime.Hours, result.Hour);
        Assert.AreEqual(AppSettings.DistributionTime.Minutes, result.Minute);
    }

    [TestMethod]
    public void IsTimeToAskPlayers_ShouldReturnTrue_WhenCurrentTimeMatchesDistributionTime()
    {
        // Arrange
        var distributionDate = DateHelper.GetNearestDistributionDateMoscowTime(DateTime.UtcNow);

        var currentDate = distributionDate.AddHours(-Constants.MOSCOW_UTC_OFFSET);

        // Act
        var result = DateHelper.IsTimeToAskPlayers(currentDate);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTimeToAskPlayers_ShouldReturnFalse_WhenCurrentTimeDoesNotMatchDistributionTime()
    {
        // Arrange
        var currentDate = DateTime.UtcNow;

        // Act
        var result = DateHelper.IsTimeToAskPlayers(currentDate);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GameStarted_ShouldReturnTrue_WhenCurrentTimeMatchesGameTime()
    {
        // Arrange
        var gameDate = DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow);

        var currentDate = gameDate.AddHours(-Constants.MOSCOW_UTC_OFFSET);

        // Act
        var result = DateHelper.GameStarted(currentDate);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void GameStarted_ShouldReturnFalse_WhenCurrentTimeDoesNotMatchGameTime()
    {
        // Arrange
        var currentDate = DateTime.UtcNow;

        // Act
        var result = DateHelper.GameStarted(currentDate);

        // Assert
        Assert.IsFalse(result);
    }
}
using Moq;
using Telegram.Bot.Types;
using TelegramFootballBot.App.Commands.AdminCommands;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Tests.Commands.AdminCommands;

[TestClass]
public class BanCommandTests
{
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();

    private BanCommand _command = null!;

    [TestInitialize]
    public void Setup()
    {
        _command = new BanCommand(_messageServiceMock.Object, _playerRepositoryMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldReturn_WhenUserIsNotBotOwner()
    {
        // Arrange
        var message = new Message
        {
            Text = "/ban 1",
            Chat = new Chat() { Id = 999 },
            From = new User { Id = 999 }
        };

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _playerRepositoryMock.Verify(x => x.GetAsync(It.IsAny<long>()), Times.Never);
        _playerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Player>()), Times.Never);
        _messageServiceMock.Verify(x => x.SendMessageToBotOwnerAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldSendPlayerNotFound_WhenPlayerIdIsInvalid()
    {
        // Arrange
        var message = CreateBotOwnerMessage("/ban invalid");

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _playerRepositoryMock.Verify(x => x.GetAsync(It.IsAny<long>()), Times.Never);
        _playerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Player>()), Times.Never);
        _messageServiceMock.Verify(x => x.SendMessageToBotOwnerAsync("Player not found"), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldBanPlayer_WhenPlayerExists()
    {
        // Arrange
        var player = new Player(123, "John", 122345);

        var message = CreateBotOwnerMessage("/ban 123");

        _playerRepositoryMock.Setup(x => x.GetAsync(123)).ReturnsAsync(player);

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        Assert.IsTrue(player.IsBanned);

        _playerRepositoryMock.Verify(x => x.UpdateAsync(player), Times.Once);
        _messageServiceMock.Verify(x => x.SendMessageToBotOwnerAsync("John was banned"), Times.Once);
    }

    private static Message CreateBotOwnerMessage(string text)
    {
        return new Message
        {
            Text = text,
            Chat = new Chat { Id = 1 },
            From = new User { Id = 1 }
        };
    }
}

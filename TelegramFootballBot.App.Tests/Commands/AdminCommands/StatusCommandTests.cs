using Moq;
using Telegram.Bot.Types;
using TelegramFootballBot.App.Commands.AdminCommands;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Tests.Commands.AdminCommands;

[TestClass]
public class StatusCommandTests
{
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();

    private StatusCommand _command = null!;

    [TestInitialize]
    public void Setup()
    {
        _command = new StatusCommand(_messageServiceMock.Object, _playerRepositoryMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldReturn_WhenUserIsNotBotOwner()
    {
        // Arrange
        var message = new Message
        {
            Text = "/status",
            Chat = new Chat { Id = 999 },
            From = new User { Id = 999 }
        };

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _playerRepositoryMock.Verify(x => x.GetAllAsync(), Times.Never);
        _messageServiceMock.Verify(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldSendStatusMessage_WhenUserIsBotOwner()
    {
        // Arrange
        var players = new List<Player>
        {
            new(1, "John", 111) { ApprovedPlayersMessageId = 10},
            new(2, "Mike", 222)
        };

        var message = CreateBotOwnerMessage("/status");
        _playerRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(players);

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _playerRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _messageServiceMock.Verify(x => x.SendMessageAsync(
                1,
                It.Is<string>(text =>
                    text.Contains("Players (2):") &&
                    text.Contains("John (1)") &&
                    text.Contains("Mike (2)") &&
                    text.Contains("Got message: 1"))),
            Times.Once);
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
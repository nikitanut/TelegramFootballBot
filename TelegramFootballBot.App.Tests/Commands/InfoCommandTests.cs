using Moq;
using Telegram.Bot.Types;
using TelegramFootballBot.App.Commands;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Tests.Commands;

[TestClass]
public class InfoCommandTests
{
    private readonly Mock<IMessageService> _messageServiceMock = new();

    private InfoCommand _command = null!;

    [TestInitialize]
    public void Setup()
    {
        _command = new InfoCommand(_messageServiceMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldSendUserCommands_WhenUserIsNotBotOwner()
    {
        // Arrange
        var message = new Message
        {
            Chat = new Chat { Id = 123 },
            From = new User { Id = 999 }
        };

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                123,
                $"/reg - зарегистрироваться{Environment.NewLine}/go - отметиться"),
            Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldSendAdminCommands_WhenUserIsBotOwner()
    {
        // Arrange
        var message = new Message
        {
            Chat = new Chat { Id = 1 },
            From = new User { Id = 1 }
        };

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                1,
                $"/status - get statistics{Environment.NewLine}/ban {{playerId}} - ban player"),
            Times.Once);
    }
}
using Moq;
using Telegram.Bot.Types;
using TelegramFootballBot.App.Commands;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Tests.Commands;

[TestClass]
public class StartCommandTests
{
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();
    private readonly Mock<ISheetService> _sheetServiceMock = new();

    private readonly CommandFactory _commandFactory;

    public StartCommandTests()
    {
        _commandFactory = new CommandFactory(_messageServiceMock.Object, _playerRepositoryMock.Object, _sheetServiceMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldSendRegistrationMessage_WhenStartCommandIsTriggered()
    {
        // Arrange
        var message = new Message 
        { 
            Text = "/start", 
            Chat = new Chat { Id = 123 }, 
            From = new User { Id = 123 } 
        };

        var command = _commandFactory.Create(message);

        // Act
        if (command is not null)
            await command.ExecuteAsync(message);

        // Assert
        Assert.IsExactInstanceOfType(command, typeof(StartCommand));
        _playerRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Player>()), Times.Never);
        _playerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Player>()), Times.Never);
        _sheetServiceMock.Verify(x => x.UpsertPlayerAsync(It.IsAny<string>()), Times.Never);
        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                123,
                "Для регистрации введите /reg Фамилия Имя"),
            Times.Once);
    }
}

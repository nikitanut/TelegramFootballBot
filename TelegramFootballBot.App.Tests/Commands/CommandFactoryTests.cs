using Moq;
using Telegram.Bot.Types;
using TelegramFootballBot.App.Commands;
using TelegramFootballBot.App.Commands.AdminCommands;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Tests.Commands;

[TestClass]
public class CommandFactoryTests
{
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();
    private readonly Mock<ISheetService> _sheetServiceMock = new();
    private readonly CommandFactory _commandFactory;

    public CommandFactoryTests()
    {
        _commandFactory = new CommandFactory(_messageServiceMock.Object, _playerRepositoryMock.Object, _sheetServiceMock.Object);
    }

    [TestMethod]
    public void Create_ShouldReturnBanCommand_WhenSendingBanRequest()
    {
        // Arrange
        var message = CreateMessage("/ban 123");

        // Act
        var command = _commandFactory.Create(message);

        // Assert
        Assert.IsExactInstanceOfType<BanCommand>(command);
    }

    [TestMethod]
    public void Create_ShouldReturnStatusCommand_WhenSendingStatusRequest()
    {
        // Arrange
        var message = CreateMessage("/status");

        // Act
        var command = _commandFactory.Create(message);

        // Assert
        Assert.IsExactInstanceOfType<StatusCommand>(command);
    }

    [TestMethod]
    public void Create_ShouldReturnGoCommand_WhenSendingGoRequest()
    {
        // Arrange
        var message = CreateMessage("/go");

        // Act
        var command = _commandFactory.Create(message);

        // Assert
        Assert.IsExactInstanceOfType<GoCommand>(command);
    }

    [TestMethod]
    public void Create_ShouldReturnInfoCommand_WhenSendingInfoRequest()
    {
        // Arrange
        var message = CreateMessage("/info");

        // Act
        var command = _commandFactory.Create(message);

        // Assert
        Assert.IsExactInstanceOfType<InfoCommand>(command);
    }

    [TestMethod]
    public void Create_ShouldReturnRegisterCommand_WhenSendingRegisterRequest()
    {
        // Arrange
        var message = CreateMessage("/reg John Smith");

        // Act
        var command = _commandFactory.Create(message);

        // Assert
        Assert.IsExactInstanceOfType<RegisterCommand>(command);
    }

    [TestMethod]
    public void Create_ShouldReturnStartCommand_WhenSendingStartRequest()
    {
        // Arrange
        var message = CreateMessage("/start");

        // Act
        var command = _commandFactory.Create(message);

        // Assert
        Assert.IsExactInstanceOfType<StartCommand>(command);
    }

    [TestMethod]
    public void Create_ShouldReturnNull_WhenSendingPlainText()
    {
        // Arrange
        var message = CreateMessage("Hello");

        // Act
        var command = _commandFactory.Create(message);

        // Assert
        Assert.IsNull(command);
    }

    private Message CreateMessage(string commandName)
    {
        return new Message
        {
            Chat = new Chat { Id = 1 },
            From = new User { Id = 1 },
            Text = commandName
        };
    }
}
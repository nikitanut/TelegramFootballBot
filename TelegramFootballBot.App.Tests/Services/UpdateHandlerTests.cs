using Moq;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.App.Commands;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Exceptions;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Models.CallbackQueries;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Tests.Services;

[TestClass]
public class UpdateHandlerTests
{
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();
    private readonly Mock<ISheetService> _sheetServiceMock = new();
    private readonly Mock<ILogger> _loggerMock = new();

    private UpdateHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        var commandFactory = new CommandFactory(
            _messageServiceMock.Object,
            _playerRepositoryMock.Object,
            _sheetServiceMock.Object);

        _handler = new UpdateHandler(
            commandFactory,
            _messageServiceMock.Object,
            _playerRepositoryMock.Object,
            _sheetServiceMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task HandleUpdateAsync_ShouldExecuteCommand_WhenMessageContainsCommand()
    {
        // Arrange
        var update = new Update
        {
            Message = CreateMessage("/info")
        };

        _playerRepositoryMock
            .Setup(x => x.GetAsync(123))
            .ReturnsAsync(new Player(123, "John", 1));

        // Act
        await _handler.HandleUpdateAsync(
            Mock.Of<ITelegramBotClient>(),
            update,
            CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                123,
                $"/reg - зарегистрироваться{Environment.NewLine}/go - отметиться"),
            Times.Once);

        _loggerMock.Verify(
            x => x.Information(
                It.Is<string>(s => s.Contains("Command /info processed for user John"))),
            Times.Once);
    }

    [TestMethod]
    public async Task HandleUpdateAsync_ShouldReturn_WhenCommandDoesNotExist()
    {
        // Arrange
        var update = new Update
        {
            Message = CreateMessage("/unknown")
        };

        // Act
        await _handler.HandleUpdateAsync(
            Mock.Of<ITelegramBotClient>(),
            update,
            CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<ReplyMarkup?>()),
            Times.Never);
    }

    [TestMethod]
    public async Task HandleUpdateAsync_ShouldHandleCommandException()
    {
        // Arrange
        var update = new Update
        {
            Message = CreateMessage("/go")
        };

        _playerRepositoryMock
            .Setup(x => x.GetAsync(123))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        await _handler.HandleUpdateAsync(
            Mock.Of<ITelegramBotClient>(),
            update,
            CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Error(
                It.IsAny<Exception>(),
                It.Is<string>(s => s.Contains("Error on processing /go command"))),
            Times.Once);

        _messageServiceMock.Verify(
            x => x.SendMessageToBotOwnerAsync(
                It.Is<string>(s => s.Contains("Ошибка у пользователя"))),
            Times.Once);

        _messageServiceMock.Verify(
            x => x.SendErrorMessageToUserAsync(123, string.Empty),
            Times.Once);
    }

    [TestMethod]
    public async Task HandleUpdateAsync_ShouldLogUnknownUpdate()
    {
        // Arrange
        var update = new Update();

        // Act
        await _handler.HandleUpdateAsync(
            Mock.Of<ITelegramBotClient>(),
            update,
            CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Information(
                "Unknown update type: {UpdateType}",
                update.Type),
            Times.Once);
    }

    [TestMethod]
    public async Task HandleUpdateAsync_ShouldReturn_WhenCallbackDataIsEmpty()
    {
        // Arrange
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Data = string.Empty
            }
        };

        // Act
        await _handler.HandleUpdateAsync(
            Mock.Of<ITelegramBotClient>(),
            update,
            CancellationToken.None);

        // Assert
        _sheetServiceMock.Verify(
            x => x.SetApproveCellAsync(
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task HandleErrorAsync_ShouldLogPollingError()
    {
        // Arrange
        var exception = new Exception("Polling error");

        // Act
        await _handler.HandleErrorAsync(
            Mock.Of<ITelegramBotClient>(),
            exception,
            HandleErrorSource.PollingError,
            CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Error(
                "Polling failed with exception: {Exception}",
                exception),
            Times.Once);
    }

    [TestMethod]
    public async Task HandleUpdateAsync_ShouldHandleUserNotFoundException_ForCallback()
    {
        // Arrange
        var callbackQuery = new CallbackQuery
        {
            Data = $"{PlayerSetCallback.Name}|{DateTime.UtcNow:O}_Да",
            From = new User
            {
                Id = 123,
                FirstName = "John",
                LastName = "Smith"
            },
            Message = new Message
            {
                Id = 10,
                Chat = new Chat { Id = 1 }
            }
        };

        var update = new Update
        {
            CallbackQuery = callbackQuery
        };

        _playerRepositoryMock
            .Setup(x => x.GetAsync(123))
            .ThrowsAsync(new UserNotFoundException());

        // Act
        await _handler.HandleUpdateAsync(Mock.Of<ITelegramBotClient>(), update, CancellationToken.None);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                1,
                "Вы не зарегистрированы. Введите команду /reg Фамилия Имя."),
            Times.Once);
    }

    private static Message CreateMessage(string text)
    {
        return new Message
        {
            Text = text,
            Chat = new Chat { Id = 123 },
            From = new User { Id = 123 }
        };
    }
}
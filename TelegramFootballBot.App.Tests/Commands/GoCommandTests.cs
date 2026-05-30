using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.App.Commands;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Exceptions;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Tests.Commands;

[TestClass]
public class GoCommandTests
{
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();

    private GoCommand _command = null!;

    [TestInitialize]
    public void Setup()
    {
        _command = new GoCommand(_messageServiceMock.Object, _playerRepositoryMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldSendRegistrationMessage_WhenUserIsNotRegistered()
    {
        // Arrange
        var message = CreateMessage();

        _playerRepositoryMock
            .Setup(x => x.GetAsync(123))
            .ThrowsAsync(new UserNotFoundException());

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                1,
                $"Вы не были зарегистрированы{Environment.NewLine}Введите /reg Фамилия Имя"),
            Times.Once);

        _messageServiceMock.Verify(
            x => x.DeleteMessageAsync(It.IsAny<ChatId>(), It.IsAny<int>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldReturn_WhenPlayerIsBanned()
    {
        // Arrange
        var player = new Player(123, "John", 1)
        {
            IsBanned = true
        };

        var message = CreateMessage();

        _playerRepositoryMock
            .Setup(x => x.GetAsync(123))
            .ReturnsAsync(player);

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _messageServiceMock.Verify(
            x => x.DeleteMessageAsync(It.IsAny<ChatId>(), It.IsAny<int>()),
            Times.Never);

        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<ReplyMarkup?>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldDeleteMessageAndSendQuestion_WhenPlayerIsValid()
    {
        // Arrange
        var player = new Player(123, "John", 777);

        var message = CreateMessage();

        _playerRepositoryMock
            .Setup(x => x.GetAsync(123))
            .ReturnsAsync(player);

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _messageServiceMock.Verify(
            x => x.DeleteMessageAsync(1, 10),
            Times.Once);

        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                777,
                It.Is<string>(text => text.StartsWith("Идёшь на футбол")),
                It.IsAny<ReplyMarkup?>()),
            Times.Once);
    }

    private static Message CreateMessage()
    {
        return new Message
        {
            Id = 10,
            Chat = new Chat { Id = 1 },
            From = new User { Id = 123 }
        };
    }
}
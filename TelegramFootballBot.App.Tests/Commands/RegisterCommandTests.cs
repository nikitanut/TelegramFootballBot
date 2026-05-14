using Moq;
using Telegram.Bot.Types;
using TelegramFootballBot.App.Commands;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Exceptions;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Tests.Commands;

[TestClass]
public class RegisterCommandTests
{
    private readonly Mock<IMessageService> _messageServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();
    private readonly Mock<ISheetService> _sheetServiceMock = new();

    private RegisterCommand _command = null!;

    [TestInitialize]
    public void Setup()
    {
        _command = new RegisterCommand(_messageServiceMock.Object, _playerRepositoryMock.Object, _sheetServiceMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldSendError_WhenPlayerNameIsEmpty()
    {
        // Arrange
        var message = CreateMessage("/reg");

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _playerRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Player>()), Times.Never);
        _playerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Player>()), Times.Never);
        _sheetServiceMock.Verify(x => x.UpsertPlayerAsync(It.IsAny<string>()), Times.Never);
        _messageServiceMock.Verify(
            x => x.SendMessageAsync(
                1,
                $"Вы не указали фамилию и имя{Environment.NewLine}Введите /reg Фамилия Имя"),
            Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldRegisterNewPlayer_WhenPlayerDoesNotExist()
    {
        // Arrange
        var message = CreateMessage("/reg John Smith");

        _playerRepositoryMock
            .Setup(x => x.GetAsync(123))
            .ThrowsAsync(new UserNotFoundException());

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        _messageServiceMock.Verify(x => x.SendMessageAsync(1, "Регистрация прошла успешно"), Times.Once);
        _sheetServiceMock.Verify(x => x.UpsertPlayerAsync("John Smith"), Times.Once);
        _messageServiceMock.Verify(x => x.SendMessageToBotOwnerAsync("John Smith зарегистрировался"), Times.Once);
        _playerRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Player>(p =>
                p.Id == 123 &&
                p.Name == "John Smith" &&
                p.ChatId == 1)),
            Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldReturnAlreadyRegistered_WhenPlayerNameDidNotChange()
    {
        // Arrange
        var existingPlayer = new Player(123, "John Smith", 1);
        var message = CreateMessage("/reg John Smith");
        _playerRepositoryMock.Setup(x => x.GetAsync(123)).ReturnsAsync(existingPlayer);

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        Assert.AreEqual("John Smith", existingPlayer.Name);

        _playerRepositoryMock.Verify(x => x.UpdateAsync(existingPlayer), Times.Once);
        _messageServiceMock.Verify(x => x.SendMessageAsync(1, "Вы уже зарегистрированы"), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldUpdatePlayerName_WhenPlayerNameChanged()
    {
        // Arrange
        var existingPlayer = new Player(123, "Old Name", 1);
        var message = CreateMessage("/reg New Name");
        _playerRepositoryMock.Setup(x => x.GetAsync(123)).ReturnsAsync(existingPlayer);

        // Act
        await _command.ExecuteAsync(message);

        // Assert
        Assert.AreEqual("New Name", existingPlayer.Name);

        _playerRepositoryMock.Verify(x => x.UpdateAsync(existingPlayer), Times.Once);
        _messageServiceMock.Verify(x => x.SendMessageAsync(1, "Вы уже были зарегистрированы. Имя обновлено."), Times.Once);
        _sheetServiceMock.Verify(x => x.UpsertPlayerAsync("New Name"), Times.Once);
        _messageServiceMock.Verify(x => x.SendMessageToBotOwnerAsync("New Name зарегистрировался"), Times.Once);
    }

    private static Message CreateMessage(string text)
    {
        return new Message
        {
            Text = text,
            Chat = new Chat { Id = 1 },
            From = new User { Id = 123 }
        };
    }
}
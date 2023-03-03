using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Core.Clients;
using TelegramFootballBot.Core.Models;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Tests.Services
{
    [TestClass]
    public class MessageServiceTests
    {
        private Mock<IBotClient> _botClientMock;
        private readonly Mock<ILogger> _loggerMock = new();
        private MessageService _messageService;

        [TestInitialize]
        public void Setup()
        {
            _botClientMock = new Mock<IBotClient>();
            _messageService = new MessageService(_botClientMock.Object, _loggerMock.Object);
        }

        [TestMethod]
        public async Task SendMessagesAsync_NoErrors_ReturnsSuccessfulResponses()
        {
            // Arrange
            var text = "Hello";
            var chats = new[] { new ChatId(1), new ChatId(2) };
            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chats[0], text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 1 } });

            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chats[1], text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 2 } });

            // Act
            var response = await _messageService.SendMessagesAsync(text, chats);

            // Assert
            Assert.AreEqual(1, response[0].ChatId);
            Assert.AreEqual(SendStatus.Success, response[0].Status);

            Assert.AreEqual(2, response[1].ChatId);
            Assert.AreEqual(SendStatus.Success, response[1].Status);
        }

        [TestMethod]
        public async Task SendMessagesAsync_ErrorOccurs_ReturnsErrorResponse()
        {
            // Arrange
            var text = "Hello";
            var errorMessage = "An error occurred";
            var chats = new[] { new ChatId(1), new ChatId(2) };
            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chats[0], text, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception(errorMessage));

            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chats[1], text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 2 } });

            // Act
            var response = await _messageService.SendMessagesAsync(text, chats);

            // Assert
            Assert.AreEqual(1, response[0].ChatId);
            Assert.AreEqual(SendStatus.Error, response[0].Status);
            Assert.AreEqual($"One or more errors occurred. ({errorMessage})", response[0].Message);

            Assert.AreEqual(2, response[1].ChatId);
            Assert.AreEqual(SendStatus.Success, response[1].Status);
        }

        [TestMethod]
        public async Task EditMessagesAsync_NoErrors_ReturnsSuccessfulResponses()
        {
            // Arrange
            var text = "Hello";
            var messages = new[]
            {
                new Message { MessageId = 1, Chat = new Chat { Id = 1 } },
                new Message { MessageId = 1, Chat = new Chat { Id = 2 } }
            };

            _botClientMock
                .Setup(m => m.EditMessageTextAsync(messages[0].Chat.Id, messages[0].MessageId, text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 1 } });

            _botClientMock
                .Setup(m => m.EditMessageTextAsync(messages[1].Chat.Id, messages[1].MessageId, text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 2 } });

            // Act
            var response = await _messageService.EditMessagesAsync(text, messages);

            // Assert
            Assert.AreEqual(1, response[0].ChatId);
            Assert.AreEqual(SendStatus.Success, response[0].Status);

            Assert.AreEqual(2, response[1].ChatId);
            Assert.AreEqual(SendStatus.Success, response[1].Status);
        }

        [TestMethod]
        public async Task EditMessagesAsync_ErrorOccurs_ReturnsErrorResponse()
        {
            // Arrange
            var text = "Hello";
            var errorMessage = "An error occurred";
            var messages = new[]
            {
                new Message { MessageId = 1, Chat = new Chat { Id = 1 } },
                new Message { MessageId = 1, Chat = new Chat { Id = 2 } }
            };

            _botClientMock
                .Setup(m => m.EditMessageTextAsync(messages[0].Chat.Id, messages[0].MessageId, text, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception(errorMessage));

            _botClientMock
                .Setup(m => m.EditMessageTextAsync(messages[1].Chat.Id, messages[1].MessageId, text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 2 } });

            // Act
            var response = await _messageService.EditMessagesAsync(text, messages);

            // Assert
            Assert.AreEqual(1, response[0].ChatId);
            Assert.AreEqual(SendStatus.Error, response[0].Status);
            Assert.AreEqual($"One or more errors occurred. ({errorMessage})", response[0].Message);

            Assert.AreEqual(2, response[1].ChatId);
            Assert.AreEqual(SendStatus.Success, response[1].Status);
        }

        [TestMethod]
        public async Task SendMessageToBotOwnerAsync_NotificationEnabled_ReturnsSentMessage()
        {
            // Arrange
            AppSettings.NotifyOwner = true;
            var text = "Hello";            
            var messageId = 1;
            _botClientMock
                .Setup(m => m.SendTextMessageAsync(AppSettings.BotOwnerChatId, text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { MessageId = messageId, Chat = new Chat { Id = AppSettings.BotOwnerChatId } });

            // Act
            var response = await _messageService.SendMessageToBotOwnerAsync(text);

            // Assert
            Assert.AreEqual(messageId, response.MessageId);
            Assert.AreEqual(AppSettings.BotOwnerChatId, response.Chat?.Id);
        }

        [TestMethod]
        public async Task SendMessageToBotOwnerAsync_NotificationDisabled_ReturnsEmptyMessage()
        {
            // Arrange
            AppSettings.NotifyOwner = false;
            var text = "Hello";            

            // Act
            var response = await _messageService.SendMessageToBotOwnerAsync(text);

            // Assert
            Assert.AreEqual(0, response.MessageId);
            Assert.IsNull(response.Chat);
        }

        [TestMethod]
        public async Task SendMessageAsync_NoErrors_ReturnsSuccessfulResponses()
        {
            // Arrange
            var text = "Hello";
            var chatId = 1;
            var messageId = 1;
            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chatId, text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { MessageId = messageId, Chat = new Chat { Id = chatId } });

            // Act
            var response = await _messageService.SendMessageAsync(chatId, text, null);

            // Assert
            Assert.AreEqual(chatId, response.Chat?.Id);
            Assert.AreEqual(messageId, response.MessageId);
        }

        [TestMethod]
        public async Task DeleteMessageAsync_ErrorOccurs_DoesNotThrowExceptionAndLogsError()
        {
            // Arrange
            var exception = new Exception("Something happened");
            var chatId = 1;
            var messageId = 1;
            _botClientMock
                .Setup(m => m.DeleteMessageAsync(chatId, messageId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            await _messageService.DeleteMessageAsync(chatId, messageId);

            // Assert
            _loggerMock.Verify(m => m.Error(exception, "An error occurred while deleting message"), Times.Once);
        }

        [TestMethod]
        public async Task SendErrorMessageToUserAsync_NoErrors_ReturnsSentMessage()
        {
            // Arrange
            var chatId = 1;
            var messageId = 1;
            var playerName = "John Smith";
            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chatId, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { MessageId = messageId, Chat = new Chat { Id = chatId } });

            // Act
            var response = await _messageService.SendErrorMessageToUserAsync(chatId, playerName);

            // Assert
            Assert.AreEqual(chatId, response.Chat?.Id);
            Assert.AreEqual(messageId, response.MessageId);
        }

        [TestMethod]
        public async Task SendErrorMessageToUserAsync_ErrorsOccurs_SendsMessageToBotOwner()
        {
            // Arrange
            var exception = new Exception("Something happened");
            var chatId = 1;
            var playerName = "John Smith";
            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chatId, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            _botClientMock
                .Setup(m => m.SendTextMessageAsync(AppSettings.BotOwnerChatId, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { MessageId = 1, Chat = new Chat { Id = AppSettings.BotOwnerChatId } });

            // Act
            var response = await _messageService.SendErrorMessageToUserAsync(chatId, playerName);

            // Assert
            Assert.AreEqual(AppSettings.BotOwnerChatId, response.Chat?.Id);
        }
    }
}

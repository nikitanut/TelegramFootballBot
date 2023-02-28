using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
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
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();
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
            var chats = new[] { new ChatId(1), new ChatId(2), new ChatId(3) };
            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chats[0], text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 1 } });

            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chats[1], text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 2 } });

            _botClientMock
                .Setup(m => m.SendTextMessageAsync(chats[2], text, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Message { Chat = new Chat { Id = 3 } });

            // Act
            var response = await _messageService.SendMessagesAsync(text, chats);

            // Assert
            Assert.AreEqual(1, response[0].ChatId);
            Assert.AreEqual(SendStatus.Success, response[0].Status);

            Assert.AreEqual(2, response[1].ChatId);
            Assert.AreEqual(SendStatus.Success, response[1].Status);

            Assert.AreEqual(3, response[2].ChatId);
            Assert.AreEqual(SendStatus.Success, response[2].Status);
        }
    }
}

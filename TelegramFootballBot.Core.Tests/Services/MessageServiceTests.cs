using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramFootballBot.Core.Clients;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Models;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Tests.Services
{
    [TestClass]
    public class MessageServiceTests
    {
        private Mock<IBotClient> _botClientMock;
        private Mock<IPlayerRepository> _playerRepositoryMock;
        private Mock<ISheetService> _sheetServiceMock;
        private Mock<ILogger> _loggerMock;
        private MessageService _messageService;

        [TestInitialize]
        public void SetUp()
        {
            _botClientMock = new Mock<IBotClient>();
            _playerRepositoryMock = new Mock<IPlayerRepository>();
            _sheetServiceMock = new Mock<ISheetService>();
            _loggerMock = new Mock<ILogger>();
            _messageService = new MessageService(_botClientMock.Object, _playerRepositoryMock.Object, _sheetServiceMock.Object, _loggerMock.Object);
        }

        [TestMethod]
        public async Task SendMessageToAllPlayersAsync_NoPlayersAvailable_ProcessesSuccessfully()
        {
            // Arrange
            _playerRepositoryMock.Setup(m => m.GetAllAsync()).ReturnsAsync(new List<Player>());

            // Act
            // Assert
            await _messageService.SendMessageToAllPlayersAsync("test");            
        }

        [TestMethod]
        public async Task SendMessageToAllPlayersAsync_PlayersAvailable_ProcessesSuccessfully()
        {
            // Arrange
            var message = "test";
            var player = new Player(1, "John", 1);
            _playerRepositoryMock.Setup(m => m.GetAllAsync()).ReturnsAsync(new List<Player> { player });

            // Act
            // Assert
            await _messageService.SendMessageToAllPlayersAsync(message);
        }
    }
}

using Telegram.Bot.Types;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Exceptions;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Models.Commands
{
    public class UnregisterCommand : Command
    {
        public override string Name => "/unreg";

        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;

        public UnregisterCommand(IMessageService messageService, IPlayerRepository playerRepository)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
        }

        public override async Task ExecuteAsync(Message message)
        {
            var playerName = await DeletePlayer(message.From!.Id);
            var messageForUser = string.IsNullOrEmpty(playerName) ? "Вы не были зарегистрированы" : "Рассылка отменена";

            await Task.WhenAll(new[]
            {
                _messageService.SendMessageAsync(message.Chat.Id, messageForUser),
                _messageService.SendMessageToBotOwnerAsync($"{playerName} отписался от рассылки")
            });
        }

        private async Task<string> DeletePlayer(long playerId)
        {
            try
            {
                var player = await _playerRepository.GetAsync(playerId);
                await _playerRepository.RemoveAsync(player.Id);
                return player.Name;
            }
            catch (UserNotFoundException)
            {
                return string.Empty;
            }
        }
    }
}

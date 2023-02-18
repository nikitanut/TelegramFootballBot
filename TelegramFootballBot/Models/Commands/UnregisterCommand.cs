using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;
using TelegramFootballBot.Data;

namespace TelegramFootballBot.Models.Commands
{
    public class UnregisterCommand : Command
    {
        public override string Name => "/unregister";

        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;

        public UnregisterCommand(IMessageService messageService, IPlayerRepository playerRepository)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
        }

        public override async Task Execute(Message message)
        {
            var playerName = await DeletePlayer(message.From.Id);
            var messageForUser = string.IsNullOrEmpty(playerName) ? "Вы не были зарегистрированы" : "Рассылка отменена";
            await _messageService.SendMessageAsync(message.Chat.Id, messageForUser);
            await _messageService.SendTextMessageToBotOwnerAsync($"{playerName} отписался от рассылки");
        }

        private async Task<string> DeletePlayer(int playerId)
        {
            try
            {
                var player = await _playerRepository.GetAsync(playerId);
                await _playerRepository.RemoveAsync(player.Id);
                return player.Name;
            }
            catch (UserNotFoundException)
            {
                return null;
            }
        }
    }
}

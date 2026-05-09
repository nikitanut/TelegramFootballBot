using Telegram.Bot.Types;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Commands.AdminCommands
{
    public class BanCommand : Command
    {
        public override string Name => "/ban";

        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;

        public BanCommand(IMessageService messageService, IPlayerRepository playerRepository)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
        }

        public override async Task ExecuteAsync(Message message)
        {
            if (!IsBotOwner(message))
                return;

            long.TryParse(message.Text, out var playerId);
            var player = playerId > 0 ? await _playerRepository.GetAsync(playerId) : null;

            if (player is null)
            {
                await _messageService.SendMessageToBotOwnerAsync("Player not found");
                return;
            }

            player.IsBanned = true;
            await _playerRepository.UpdateAsync(player);
            await _messageService.SendMessageToBotOwnerAsync($"{player.Name} was banned");
        }
    }
}

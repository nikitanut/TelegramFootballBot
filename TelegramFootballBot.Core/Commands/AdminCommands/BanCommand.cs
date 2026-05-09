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

            var playerId = GetPlayerId(message);
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

        private long GetPlayerId(Message message)
        {
            var playerIdString = message.Text!.Length > Name.Length ? message.Text[Name.Length..].Trim() : string.Empty;
            long.TryParse(playerIdString, out var playerId);
            return playerId;
        }
    }
}

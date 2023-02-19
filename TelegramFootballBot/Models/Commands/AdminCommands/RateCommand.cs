using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Core.Services;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Exceptions;

namespace TelegramFootballBot.Core.Models.Commands.AdminCommands
{
    public class RateCommand : Command
    {
        public override string Name => "/rate";

        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;

        public RateCommand(IMessageService messageService, IPlayerRepository playerRepository)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
        }

        public override async Task ExecuteAsync(Message message)
        {
            if (!IsBotOwner(message))
                return;

            if (!TryParse(message, out string playerName, out int rating))
            {
                await _messageService.SendMessageAsync(message.Chat.Id, "Wrong rating string. Example: /rate playerName rating");
                return;
            }

            var player = await FindPlayer(_playerRepository, playerName);
            if (player == null)
            {
                await _messageService.SendMessageAsync(message.Chat.Id, "Player not found");
                return;
            }

            player.Rating = rating;
            await _playerRepository.UpdateAsync(player);
            await _messageService.SendMessageAsync(message.Chat.Id, $"{player.Name} - {player.Rating}");
        }

        private bool TryParse(Message message, out string name, out int rating)
        {
            name = null;
            rating = 0;

            var ratingString = message.Text.Length > Name.Length
                ? message.Text[Name.Length..].Trim()
                : string.Empty;

            var ratingSeparatorIndex = ratingString.LastIndexOf(' ');
            if (ratingSeparatorIndex == -1) return false;

            name = ratingString.Substring(0, ratingSeparatorIndex);
            return int.TryParse(ratingString[(ratingSeparatorIndex + 1)..], out rating);
        }

        private static async Task<Player> FindPlayer(IPlayerRepository playerRepository, string playerName)
        {
            try
            {
                return await playerRepository.GetAsync(playerName);
            }
            catch (UserNotFoundException)
            {
                return null;
            }
        }
    }
}

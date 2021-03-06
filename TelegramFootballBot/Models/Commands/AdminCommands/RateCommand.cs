﻿using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Data;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class RateCommand : Command
    {
        public override string Name => "/rate";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (!IsBotOwner(message))
                return;

            string playerName;
            int rating;

            if (!TryParse(message, out playerName, out rating))
            {
                await messageController.SendMessageAsync(message.Chat.Id, "Wrong rating string. Example: /rate playerName rating");
                return;
            }

            var player = await FindPlayer(messageController.PlayerRepository, playerName);
            if (player == null)
            {
                await messageController.SendMessageAsync(message.Chat.Id, "Player not found");
                return;
            }

            player.Rating = rating;
            await messageController.PlayerRepository.UpdateAsync(player);
            await messageController.SendMessageAsync(message.Chat.Id, $"{player.Name} - {player.Rating}");
        }

        private bool TryParse(Message message, out string name, out int rating)
        {
            name = null;
            rating = 0;

            var ratingString = message.Text.Length > Name.Length
                ? message.Text.Substring(Name.Length).Trim()
                : string.Empty;

            var ratingSeparatorIndex = ratingString.LastIndexOf(' ');
            if (ratingSeparatorIndex == -1) return false;

            name = ratingString.Substring(0, ratingSeparatorIndex);
            return int.TryParse(ratingString.Substring(ratingSeparatorIndex + 1), out rating);
        }

        private async Task<Player> FindPlayer(IPlayerRepository playerRepository, string playerName)
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

using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Data;

namespace TelegramFootballBot.Models.Commands
{
    public class UnregisterCommand : Command
    {
        public override string Name => "/unregister";

        public override async Task Execute(Message message, MessageController messageController)
        {
            var playerName = await DeletePlayer(messageController.PlayerRepository, message.From.Id);
            var messageForUser = string.IsNullOrEmpty(playerName) ? "Вы не были зарегистрированы" : "Рассылка отменена";
            await messageController.SendMessageAsync(message.Chat.Id, messageForUser);
            await messageController.SendTextMessageToBotOwnerAsync($"{playerName} отписался от рассылки");
        }

        private async Task<string> DeletePlayer(IPlayerRepository playerRepository, int playerId)
        {
            try
            {
                var player = await playerRepository.GetAsync(playerId);
                await playerRepository.RemoveAsync(player.Id);
                return player.Name;
            }
            catch (UserNotFoundException)
            {
                return null;
            }
        }
    }
}

using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;
using TelegramFootballBot.Data;

namespace TelegramFootballBot.Models.Commands
{
    public class UnregisterCommand : Command
    {
        public override string Name => "/unregister";

        public override async Task Execute(Message message, MessageService messageService)
        {
            var playerName = await DeletePlayer(messageService.PlayerRepository, message.From.Id);
            var messageForUser = string.IsNullOrEmpty(playerName) ? "Вы не были зарегистрированы" : "Рассылка отменена";
            await messageService.SendMessageAsync(message.Chat.Id, messageForUser);
            await messageService.SendTextMessageToBotOwnerAsync($"{playerName} отписался от рассылки");
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

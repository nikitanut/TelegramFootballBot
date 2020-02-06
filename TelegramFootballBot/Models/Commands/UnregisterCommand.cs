using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands
{
    public class UnregisterCommand : Command
    {
        public override string Name => "/unregister";

        public override async Task Execute(Message message, MessageController messageController)
        {
            var playerName = string.Empty;
            var messageForUser = "Рассылка отменена";

            try
            {
                playerName = (await messageController.PlayerRepository.GetAsync(message.From.Id)).Name;
                await messageController.PlayerRepository.RemoveAsync(message.From.Id);
            }
            catch (UserNotFoundException)
            {
                messageForUser = "Вы не были зарегистрированы";
            }

            await messageController.SendMessageAsync(message.Chat.Id, messageForUser);
            await messageController.SendTextMessageToBotOwnerAsync($"{playerName} отписался от рассылки");
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class UnregisterCommand : Command
    {
        public override string Name => "/unregister";

        public override async Task Execute(Message message, TelegramBotClient client)
        {
            var activePlayer = Bot.Players.FirstOrDefault(p => p.Id == message.From.Id && p.IsActive);
            if (activePlayer != null)
            {
                activePlayer.IsActive = false;
                Bot.UpdatePlayers();  
            }

            var messageForUser = activePlayer != null
                ? "Рассылка отменена"
                : "Вы не были зарегистрированы";

            await client.SendTextMessageWithTokenAsync(message.Chat.Id, messageForUser);
            await client.SendTextMessageToBotOwnerAsync($"{activePlayer.Name} отписался от рассылки");
        }
    }
}

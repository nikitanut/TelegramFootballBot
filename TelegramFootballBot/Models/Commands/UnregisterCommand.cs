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
            Player player = null;
            var messageForUser = "Рассылка отменена";

            try
            {
                player = await Bot.GetPlayerAsync(message.From.Id);
            }
            catch (UserNotFoundException)
            {
            }

            if (player?.IsActive == true)
            {
                player.IsActive = false;
                await Bot.UpdatePlayerAsync(player);
            }
            else
                messageForUser = "Вы не были зарегистрированы";

            await client.SendTextMessageWithTokenAsync(message.Chat.Id, messageForUser);
            await client.SendTextMessageToBotOwnerAsync($"{player.Name} отписался от рассылки");
        }
    }
}

using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Data;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class UnregisterCommand : Command
    {
        public override string Name => "/unregister";

        public override async Task Execute(Message message, TelegramBotClient client)
        {
            var playerName = string.Empty;
            var messageForUser = "Рассылка отменена";

            try
            {
                IPlayerRepository playerRepository = new PlayerRepository();
                playerName = (await playerRepository.GetAsync(message.From.Id)).Name;
                await playerRepository.RemoveAsync(message.From.Id);
            }
            catch (UserNotFoundException)
            {
                messageForUser = "Вы не были зарегистрированы";
            }

            await client.SendTextMessageWithTokenAsync(message.Chat.Id, messageForUser);

            if (AppSettings.NotifyOwner)
                await client.SendTextMessageToBotOwnerAsync($"{playerName} отписался от рассылки");
        }
    }
}

using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class SwitchNotifierCommand : Command
    {
        public override string Name => "/switch";

        public override async Task Execute(Message message, TelegramBotClient client)
        {
            if (message.Chat.Id != AppSettings.BotOwnerChatId)
                return;

            AppSettings.NotifyOwner = !AppSettings.NotifyOwner;
            await client.SendTextMessageWithTokenAsync(message.Chat.Id, AppSettings.NotifyOwner ? "On" : "Off");
        }
    }
}

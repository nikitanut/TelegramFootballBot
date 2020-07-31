using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class SwitchNotifierCommand : Command
    {
        public override string Name => "/switch";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (!IsBotOwner(message))
                return;

            AppSettings.NotifyOwner = !AppSettings.NotifyOwner;
            await messageController.SendTextMessageToBotOwnerAsync(AppSettings.NotifyOwner ? "On" : "Off");
        }
    }
}

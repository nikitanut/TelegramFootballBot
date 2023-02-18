using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class SwitchNotifierCommand : Command
    {
        public override string Name => "/switch";

        public override async Task Execute(Message message, MessageService messageService)
        {
            if (!IsBotOwner(message))
                return;

            AppSettings.NotifyOwner = !AppSettings.NotifyOwner;
            await messageService.SendTextMessageToBotOwnerAsync(AppSettings.NotifyOwner ? "On" : "Off");
        }
    }
}

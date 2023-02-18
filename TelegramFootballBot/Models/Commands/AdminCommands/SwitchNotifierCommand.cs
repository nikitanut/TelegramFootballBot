using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class SwitchNotifierCommand : Command
    {
        public override string Name => "/switch";

        private readonly IMessageService _messageService;

        public SwitchNotifierCommand(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task Execute(Message message)
        {
            if (!IsBotOwner(message))
                return;

            AppSettings.NotifyOwner = !AppSettings.NotifyOwner;
            await _messageService.SendTextMessageToBotOwnerAsync(AppSettings.NotifyOwner ? "On" : "Off");
        }
    }
}

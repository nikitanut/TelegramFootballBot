using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Models.Commands.AdminCommands
{
    public class SwitchNotifierCommand : Command
    {
        public override string Name => "/switch";

        private readonly IMessageService _messageService;

        public SwitchNotifierCommand(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task ExecuteAsync(Message message)
        {
            if (!IsBotOwner(message))
                return;

            AppSettings.NotifyOwner = !AppSettings.NotifyOwner;
            await _messageService.SendMessageToBotOwnerAsync(AppSettings.NotifyOwner ? "On" : "Off");
        }
    }
}

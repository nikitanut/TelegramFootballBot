using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Models.Commands.AdminCommands
{
    public class SayCommand : Command
    {
        public override string Name => "/say";

        private readonly IMessageService _messageService;

        public SayCommand(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task Execute(Message message)
        {
            if (!IsBotOwner(message))
                return;

            var text = message.Text.Length > Name.Length
                ? message.Text[Name.Length..].Trim()
                : string.Empty;

            if (text != string.Empty)
                await _messageService.SendMessageToAllUsersAsync(text);
        }
    }
}

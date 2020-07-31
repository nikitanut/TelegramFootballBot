using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class SayCommand : Command
    {
        public override string Name => "/say";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (!IsBotOwner(message))
                return;

            var text = message.Text.Length > Name.Length
                ? message.Text.Substring(Name.Length).Trim()
                : string.Empty;

            if (text != string.Empty)
                await messageController.SendMessageToAllUsersAsync(text);
        }
    }
}

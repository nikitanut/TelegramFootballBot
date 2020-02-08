using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands
{
    public class PingCommand : Command
    {
        public override string Name => "/ping";

        public override async Task Execute(Message message, MessageController messageController)
        {
            await messageController.SendMessageAsync(message.Chat.Id, "pong");
        }
    }
}

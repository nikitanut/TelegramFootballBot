using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands
{
    public class StartCommand : Command
    {
        public override string Name => "/start";

        public override async Task Execute(Message message, MessageController messageController)
        {
            await messageController.SendMessageAsync(message.Chat.Id, "Для регистрации введите /reg Фамилия Имя");
        }
    }
}

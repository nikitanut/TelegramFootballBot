using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;

namespace TelegramFootballBot.Models.Commands
{
    public class StartCommand : Command
    {
        public override string Name => "/start";

        public override async Task Execute(Message message, MessageService messageService)
        {
            await messageService.SendMessageAsync(message.Chat.Id, "Для регистрации введите /reg Фамилия Имя");
        }
    }
}

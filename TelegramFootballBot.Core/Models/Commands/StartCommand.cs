using Telegram.Bot.Types;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Models.Commands
{
    public class StartCommand : Command
    {
        public override string Name => "/start";

        private readonly IMessageService _messageService;

        public StartCommand(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task ExecuteAsync(Message message)
        {
            await _messageService.SendMessageAsync(message.Chat.Id, "Для регистрации введите /reg Фамилия Имя");
        }
    }
}

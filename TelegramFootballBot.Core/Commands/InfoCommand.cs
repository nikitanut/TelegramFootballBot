using Telegram.Bot.Types;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Commands
{
    public class InfoCommand : Command
    {
        public override string Name => "/info";

        private readonly IMessageService _messageService;

        public InfoCommand(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task ExecuteAsync(Message message)
        {
            await _messageService.SendMessageAsync(message.Chat.Id, Text(message));
        }

        private static string Text(Message message)
        {
            if (IsBotOwner(message))
            {
                return $"/status - get statistics{Environment.NewLine}" +
                       $"/switch - turn on / turn off notifications";
            }

            return $"/reg - зарегистрироваться{Environment.NewLine}" +
                   $"/go - отметиться";
        }
    }
}

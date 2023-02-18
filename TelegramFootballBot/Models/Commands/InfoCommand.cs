using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;

namespace TelegramFootballBot.Models.Commands
{
    public class InfoCommand : Command
    {
        public override string Name => "/info";

        private readonly IMessageService _messageService;

        public InfoCommand(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task Execute(Message message)
        {
            await _messageService.SendMessageAsync(message.Chat.Id, Text(message));
        }

        private string Text(Message message)
        {
            if (IsBotOwner(message))
            {
                return $"/distribute - run distribution{Environment.NewLine}" +
                       $"/list - list of registered players{Environment.NewLine}" +
                       $"/rate - set player rating{Environment.NewLine}" +
                       $"/say - send text to all players{Environment.NewLine}" +
                       $"/status - get statistics{Environment.NewLine}" +
                       $"/switch - turn on / turn off notifications";                
            }
            
            return $"/reg - зарегистрироваться{Environment.NewLine}" +
                   $"/unregister - отписаться от рассылки{Environment.NewLine}" +
                   $"/go - отметиться";
        }
    }
}

using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands
{
    public class InfoCommand : Command
    {
        public override string Name => "/info";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (IsBotOwner(message))
            {
                var text = $"/distribute - run distribution{Environment.NewLine}" +
                           $"/list - list of registered players{Environment.NewLine}" +
                           $"/rate - set player rating{Environment.NewLine}" +
                           $"/say - send text to all players{Environment.NewLine}" +
                           $"/status - get statistics{Environment.NewLine}" +
                           $"/switch - turn on / turn off notifications";

                await messageController.SendMessageAsync(message.Chat.Id, text);
            }

            if (!IsBotOwner(message))
            {
                var text = $"/reg - зарегистрироваться{Environment.NewLine}" +
                           $"/unregister - отписаться от рассылки{Environment.NewLine}" +
                           $"/go - отметиться";

                await messageController.SendMessageAsync(message.Chat.Id, text);
            }
        }
    }
}

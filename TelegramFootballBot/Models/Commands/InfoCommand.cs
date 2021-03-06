﻿using System;
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
            await messageController.SendMessageAsync(message.Chat.Id, Text(message));
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

﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class UnregisterCommand : Command
    {
        public override string Name => "/unregister";

        public override async Task Execute(Message message, TelegramBotClient client)
        {
            var activePlayer = Bot.Players.FirstOrDefault(p => p.Id == message.From.Id && p.IsActive);
            if (activePlayer != null)
            {
                activePlayer.IsActive = false;
                await Bot.UpdatePlayersAsync();  
            }

            var messageForUser = activePlayer != null
                ? "Рассылка отменена"
                : "Вы не были зарегистрированы";

            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await client.SendTextMessageAsync(message.Chat.Id, messageForUser, cancellationToken: cancellationToken);                          
        }
    }
}

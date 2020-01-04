using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class ListCommand : Command
    {
        public override string Name => "/list";

        public override async Task Execute(Message message, TelegramBotClient client)
        {
            var readyPlayers = await SheetController.GetInstance().GetReadyPlayersAsync();
            var text = readyPlayers.Count > 0
                ? string.Join(Environment.NewLine, readyPlayers)
                : "Никто не отметился";

            await client.SendTextMessageWithTokenAsync(message.Chat.Id, text);
        }
    }
}

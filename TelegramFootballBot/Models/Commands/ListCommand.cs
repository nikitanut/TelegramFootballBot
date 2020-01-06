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
            var approvedPlayersMessage = await SheetController.GetInstance().GetApprovedPlayersMessageAsync();
            await client.SendTextMessageWithTokenAsync(message.Chat.Id, approvedPlayersMessage);
        }
    }
}

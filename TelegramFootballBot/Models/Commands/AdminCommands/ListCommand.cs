using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class ListCommand : Command
    {
        public override string Name => "/list";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (!IsBotOwner(message))
                return;

            var playersNames = (await messageController.PlayerRepository.GetAllAsync())
                .Select(p => p.Name).OrderBy(p => p);

            var text = string.Join(Environment.NewLine, playersNames);
            await messageController.SendMessageAsync(message.Chat.Id, text);
        }
    }
}

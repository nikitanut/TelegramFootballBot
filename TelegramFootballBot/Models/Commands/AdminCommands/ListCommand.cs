using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class ListCommand : Command
    {
        public override string Name => "/list";

        public override async Task Execute(Message message, MessageService messageService)
        {
            if (!IsBotOwner(message))
                return;

            var players = (await messageService.PlayerRepository.GetAllAsync()).OrderBy(p => p.Name);

            var text = string.Join(Environment.NewLine, players.Select(p => $"{p.Name} - {p.Rating}"));
            await messageService.SendMessageAsync(message.Chat.Id, text);
        }
    }
}

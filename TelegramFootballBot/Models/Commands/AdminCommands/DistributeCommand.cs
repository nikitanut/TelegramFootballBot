using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class DistributeCommand : Command
    {
        public override string Name => "/distribute";

        public override async Task Execute(Message message, MessageService messageService)
        {
            if (!IsBotOwner(message))
                return;

            await messageService.SendDistributionQuestionAsync();
        }
    }
}

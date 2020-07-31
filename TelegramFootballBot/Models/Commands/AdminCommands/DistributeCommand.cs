using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class DistributeCommand : Command
    {
        public override string Name => "/distribute";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (!IsBotOwner(message))
                return;

            await messageController.SendDistributionQuestionAsync();
        }
    }
}

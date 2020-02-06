using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands
{
    public class DistributeCommand : Command
    {
        public override string Name => "/distribute";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (message.Chat.Id != AppSettings.BotOwnerChatId)
                return;

            await messageController.SendDistributionQuestionAsync();
        }
    }
}

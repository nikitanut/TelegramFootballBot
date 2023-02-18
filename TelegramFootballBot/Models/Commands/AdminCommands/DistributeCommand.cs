using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Models.Commands.AdminCommands
{
    public class DistributeCommand : Command
    {
        public override string Name => "/distribute";

        private readonly IMessageService _messageService;

        public DistributeCommand(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task Execute(Message message)
        {
            if (!IsBotOwner(message))
                return;

            await _messageService.SendDistributionQuestionAsync();
        }
    }
}

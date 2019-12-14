using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class HelloCommand : Command
    {
        public override string Name => "hello";

        public override async Task Execute(Message message, TelegramBotClient client)
        {
            var chatId = message.Chat.Id;
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await client.SendTextMessageAsync(chatId, "Hi", cancellationToken: cancellationToken);
        }
    }
}

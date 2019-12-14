using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class StartCommand : Command
    {
        public override string Name => "/start";
        
        public override async Task Execute(Message message, TelegramBotClient client)
        {
            var chatId = message.Chat.Id;
            var currentPlayer = Bot.Players.FirstOrDefault(p => p.Id == message.From.Id);
            if (currentPlayer == null)
            {
                var cancellationTokenRegister = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                await client.SendTextMessageAsync(chatId, "Введите /register *Фамилия* *Имя*", cancellationToken: cancellationTokenRegister);
            }

            // TODO: setActive on /start
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await client.SendTextMessageAsync(chatId, "Идёшь на футбол?", cancellationToken: cancellationToken);
        }
    }
}

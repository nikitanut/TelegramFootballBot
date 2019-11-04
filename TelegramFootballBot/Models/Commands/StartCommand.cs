using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramFootballBot.Models.Commands
{
    public class StartCommand : Command
    {
        public override string Name => @"/start";
        
        public override async Task Execute(Message message, TelegramBotClient client)
        {
            var chatId = message.Chat.Id;
            var currentPlayer = Bot.Players.FirstOrDefault(p => p.Id == message.From.Id);
            if (currentPlayer == null)
            {
                await client.SendTextMessageAsync(chatId, "Введите /register *Фамилия* *Имя*");
            }

            // TODO: setActive on /start

            await client.SendTextMessageAsync(chatId, "Идёшь на футбол?");
        }
    }
}

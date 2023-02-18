using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TelegramFootballBot.Core.Models.Commands
{
    public abstract class Command
    {
        public abstract string Name { get; }

        public abstract Task Execute(Message message);

        public bool StartsWith(Message message)
        {
            return message.Type == Telegram.Bot.Types.Enums.MessageType.Text
                ? message.Text.StartsWith(Name)
                : false;
        }

        public bool IsBotOwner(Message message)
        {
            return message.Chat.Id == AppSettings.BotOwnerChatId;
        }
    }
}

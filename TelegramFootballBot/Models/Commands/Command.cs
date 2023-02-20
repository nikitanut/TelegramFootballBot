using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramFootballBot.Core.Models.Commands
{
    public abstract class Command
    {
        public abstract string Name { get; }

        public abstract Task ExecuteAsync(Message message);

        public bool StartsWith(Message message)
        {
            return message.Type == MessageType.Text
                && message.Text.StartsWith(Name);
        }

        public static bool IsBotOwner(Message message)
        {
            return message.Chat.Id == AppSettings.BotOwnerChatId;
        }
    }
}

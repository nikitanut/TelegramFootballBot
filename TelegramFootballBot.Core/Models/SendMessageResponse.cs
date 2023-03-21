using Telegram.Bot.Types;

namespace TelegramFootballBot.Core.Models
{
    public class SendMessageResponse
    {
        public ChatId ChatId { get; set; }

        public SendStatus Status { get; set; }

        public string Message { get; set; }
    }

    public enum SendStatus
    {
        Success = 1,
        Error = 2
    }
}

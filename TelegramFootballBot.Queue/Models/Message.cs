using MediatR;

namespace TelegramFootballBot.Queue.Models
{
    public class Message : IRequest
    {
        public Message()
        {
        }

        public virtual string Type { get; }
    }
}

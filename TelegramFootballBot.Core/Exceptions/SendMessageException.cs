namespace TelegramFootballBot.Core.Exceptions
{
    public class SendMessageException : ApplicationException
    {
        public SendMessageException(string message) : base(message)
        {
        }
    }
}

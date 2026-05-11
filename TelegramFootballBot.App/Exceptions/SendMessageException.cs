namespace TelegramFootballBot.App.Exceptions;

public class SendMessageException(string message) : ApplicationException(message)
{
}

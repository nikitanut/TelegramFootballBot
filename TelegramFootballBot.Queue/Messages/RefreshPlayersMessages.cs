using TelegramFootballBot.Queue.Models;

namespace TelegramFootballBot.Queue.Messages
{
    public class RefreshPlayersMessages : Message
    {
        public RefreshPlayersMessages()
        {

        }

        public override string Type => nameof(RefreshPlayersMessages);
    }
}

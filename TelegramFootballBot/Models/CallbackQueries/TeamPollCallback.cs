using System;
using System.Linq;
using TelegramFootballBot.Core.Helpers;

namespace TelegramFootballBot.Core.Models.CallbackQueries
{
    public class TeamPollCallback : Callback
    {
        public static string Name => "TeamPoll";
        public Guid PollId { get; private set; }

        public TeamPollCallback(string callbackData) : base(callbackData)
        {
            PollId = GetPollId(callbackData);
        }

        public static string GetCallbackPrefix(Guid activePollId)
        {
            return Name + Constants.CALLBACK_DATA_SEPARATOR + activePollId;
        }

        private static Guid GetPollId(string callbackData)
        {
            var pollString = Prefix(callbackData).Split(Constants.CALLBACK_DATA_SEPARATOR).Last();
            if (!Guid.TryParse(pollString, out Guid pollId))
                throw new ArgumentException($"Poll id was not provided for callback data: {callbackData}");
            return pollId;
        }
    }
}

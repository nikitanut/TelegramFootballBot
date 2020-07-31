namespace TelegramFootballBot.Helpers
{
    public static class Constants
    {
        public const char CALLBACK_PREFIX_SEPARATOR = '_';
        public const char CALLBACK_PREFIX_DATA_SEPARATOR = '|';
        
        public const string YES_ANSWER = "Да";
        public const string NO_ANSWER = "Нет";
        public const string MAYBE_ANSWER = "+ / -";
        public const string LIKE_EMOJI = "👍🏼";
        public const string DISLIKE_EMOJI = "👎🏼";
        public const string APPROVED_PLAYERS_MESSAGE_TYPE = "ApprovedPlayersMessage";
        public const string TEAM_POLL_MESSAGE_TYPE = "TeamPollMessage";

        public const int MOSCOW_UTC_OFFSET = 3;
        public const int DEFAULT_PLAYER_RATING = 73;
        public const int ASYNC_OPERATION_TIMEOUT = 10000;
        public const int TEAM_VARIANTS_TO_GENERATE = 8;
    }
}

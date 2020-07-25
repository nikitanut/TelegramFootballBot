namespace TelegramFootballBot.Helpers
{
    public static class Constants
    {
        public const string PLAYERS_SET_CALLBACK_PREFIX = "PlayersSetDetermination";
        public const string TEAM_POLL_CALLBACK_PREFIX = "TeamPoll";
        public const char PLAYERS_SET_CALLBACK_PREFIX_SEPARATOR = '_';
        public const char CALLBACK_DATA_SEPARATOR = '|';
        public const int ASYNC_OPERATION_TIMEOUT = 10000;
        public const string YES_ANSWER = "Да";
        public const string NO_ANSWER = "Нет";
        public const string MAYBE_ANSWER = "+ / -";
        public const int MOSCOW_UTC_OFFSET = 3;
        public const int DEFAULT_PLAYER_RATING = 73;
    }
}

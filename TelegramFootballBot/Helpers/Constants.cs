namespace TelegramFootballBot.Core.Helpers
{
    public static class Constants
    {
        public const char CALLBACK_PREFIX_SEPARATOR = '_';
        public const char CALLBACK_DATA_SEPARATOR = '|';
        
        public const string YES_ANSWER = "Да";
        public const string NO_ANSWER = "Нет";
        public const string MAYBE_ANSWER = "+ / -";

        public const int MOSCOW_UTC_OFFSET = 3;
        public const int ASYNC_OPERATION_TIMEOUT = 10000;
    }
}

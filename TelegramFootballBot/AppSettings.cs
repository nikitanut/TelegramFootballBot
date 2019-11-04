using System;

namespace TelegramFootballBot
{
    public static class AppSettings
    {
        public static string BotToken => "";

        public static string BotName => "";

        public static string GoogleDocUrl => "";

        public static TimeSpan DistributionTime => new TimeSpan(days: 7, hours: 15, minutes: 1, seconds: 0);

        public static TimeSpan GameDay => new TimeSpan(days: 5, hours: 20, minutes: 0, seconds: 0);
    }
}

using Microsoft.Extensions.Configuration;
using System;

namespace TelegramFootballBot
{
    public static class AppSettings
    {
        private static readonly IConfiguration _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        public static string BotToken => _configuration["botToken"];

        public static string BotName => _configuration["botName"];

        public static string GoogleDocSheetId => _configuration["googleDocSheetId"];

        public static TimeSpan DistributionTime => TimeSpan.Parse(_configuration["distributionTime"]);

        public static TimeSpan GameDay => TimeSpan.Parse(_configuration["gameDay"]);

        public static int BotOwnerChatId => int.Parse(_configuration["BotOwnerChatId"]);
    }
}

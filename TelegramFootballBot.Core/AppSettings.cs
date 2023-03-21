using Microsoft.Extensions.Configuration;

namespace TelegramFootballBot.Core
{
    public static class AppSettings
    {
        private static readonly IConfiguration _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true).Build();

        public static TimeSpan DistributionTime => TimeSpan.Parse(_configuration["distributionTime"]);

        public static TimeSpan GameDay => TimeSpan.Parse(_configuration["gameDay"]);

        public static int BotOwnerChatId => int.Parse(_configuration["botOwnerChatId"]);

        public static bool NotifyOwner { get; set; } = true;
    }
}

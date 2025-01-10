using Microsoft.Extensions.Configuration;

namespace TelegramFootballBot.Core
{
    public static class AppSettings
    {
        private static readonly IConfiguration _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true).Build();

        public static TimeSpan DistributionTime => TimeSpan.Parse(_configuration["distributionTime"] ?? throw new ApplicationException("distributionTime is empty"));

        public static TimeSpan GameDay => TimeSpan.Parse(_configuration["gameDay"] ?? throw new ApplicationException("gameDay is empty"));

        public static int BotOwnerChatId => int.Parse(_configuration["botOwnerChatId"] ?? throw new ApplicationException("botOwnerChatId is empty"));

        public static bool NotifyOwner { get; set; } = true;
    }
}

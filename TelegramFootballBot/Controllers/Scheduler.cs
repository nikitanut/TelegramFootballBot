using Serilog;
using System;
using System.Threading;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Controllers
{
    public class Scheduler
    {
        private readonly Timer _timer;
        private readonly MessageController _messageController;
        private readonly ILogger _logger;

        public Scheduler(MessageController messageController, ILogger logger)
        {
            _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite); 
            _messageController = messageController;
            _logger = logger;
        }

        public void Run()
        {
            var interval = 60 * 1000;
            _timer.Change(0, interval); 
        }

        private async void OnTimerElapsed(object sender)
        {
            var now = DateTime.UtcNow;

            if (DistributionTimeHasCome(now))
                await _messageController.StartPlayersSetDeterminationAsync();

            if (NeedToUpdateTotalPlayers(now))
                await _messageController.UpdateTotalPlayersMessagesAsync();

            if (GameStarted(now))
                await _messageController.ClearGameAttrsAsync();
        }

        private bool DistributionTimeHasCome(DateTime date)
        {
            return GetDayOfWeek(date.ToMoscowTime()) == AppSettings.DistributionTime.Days
                && date.ToMoscowTime().TimeOfDay.Hours == AppSettings.DistributionTime.Hours
                && date.ToMoscowTime().TimeOfDay.Minutes == AppSettings.DistributionTime.Minutes;
        }

        private bool NeedToUpdateTotalPlayers(DateTime date)
        {
            return date.ToMoscowTime() > GetDistributionDateMoscowTime() 
                && date.ToMoscowTime() < GetGameDateMoscowTime(date);
        }

        private bool GameStarted(DateTime date)
        {
            var gameDate = GetGameDateMoscowTime(date.ToMoscowTime());
            return date.ToMoscowTime().Year == gameDate.Year 
                && date.ToMoscowTime().Month == gameDate.Month 
                && date.ToMoscowTime().Hour == gameDate.Hour 
                && date.ToMoscowTime().Minute == gameDate.Minute;
        }
        
        public static DateTime GetGameDateMoscowTime(DateTime date)
        {
            var gameDate = date.ToMoscowTime().Date;
            var dayOfWeek = GetDayOfWeek(gameDate);

            while (AppSettings.GameDay.Days != dayOfWeek)
            {
                gameDate = gameDate.AddDays(1);
                dayOfWeek = GetDayOfWeek(gameDate);
            }

            gameDate = gameDate.AddHours(AppSettings.GameDay.Hours).AddMinutes(AppSettings.GameDay.Minutes);
            return gameDate.ToUniversalTime() > DateTime.UtcNow ? gameDate : gameDate.AddDays(7);
        }

        private DateTime GetDistributionDateMoscowTime()
        {
            var distributionDate = DateTime.UtcNow.ToMoscowTime();
            var dayOfWeek = GetDayOfWeek(distributionDate);

            while (AppSettings.DistributionTime.Days != dayOfWeek)
            {
                distributionDate = distributionDate.AddDays(1);
                dayOfWeek = GetDayOfWeek(distributionDate);
            }

            return distributionDate;
        }

        private static int GetDayOfWeek(DateTime date)
        {
            return date.DayOfWeek != 0 ? (int)date.DayOfWeek : 7;
        }
    }
}

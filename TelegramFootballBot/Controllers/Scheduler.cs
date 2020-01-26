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

        private async void OnTimerElapsed(object state)
        {
            var now = DateTime.UtcNow;

            if (DistributionTimeHasCome(now))
                await _messageController.StartPlayersSetDeterminationAsync();

            if (NeedToUpdateTotalPlayers(now))
                await _messageController.UpdateTotalPlayersMessagesAsync();

            if (GameStarted(now))
            {
                try
                {
                    await SheetController.GetInstance().ClearGameAttrsAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Excel-file updating error");
                    _messageController.SendTextMessageToBotOwnerAsync("Ошибка при обновлении excel-файла");
                }
            }
        }

        private bool DistributionTimeHasCome(DateTime now)
        {
            return GetDayOfWeek(now.ToMoscowTime()) == AppSettings.DistributionTime.Days
                && now.ToMoscowTime().TimeOfDay.Hours == AppSettings.DistributionTime.Hours
                && now.ToMoscowTime().TimeOfDay.Minutes == AppSettings.DistributionTime.Minutes;
        }

        private bool NeedToUpdateTotalPlayers(DateTime now)
        {
            return now.ToMoscowTime() > GetDistributionDateMoscowTime(now) 
                && now.ToMoscowTime() < GetGameDateMoscowTime(now);
        }

        private bool GameStarted(DateTime now)
        {
            var gameDate = GetGameDateMoscowTime(now.ToMoscowTime());
            return now.ToMoscowTime().Year == gameDate.Year 
                && now.ToMoscowTime().Month == gameDate.Month 
                && now.ToMoscowTime().Hour == gameDate.Hour 
                && now.ToMoscowTime().Minute == gameDate.Minute;
        }
        
        public static DateTime GetGameDateMoscowTime(DateTime now)
        {
            return GetNearestDate(now, 
                AppSettings.GameDay.Days, 
                AppSettings.GameDay.Hours, 
                AppSettings.GameDay.Minutes);
        }

        private DateTime GetDistributionDateMoscowTime(DateTime now)
        {
            return GetNearestDate(now, 
                AppSettings.DistributionTime.Days, 
                AppSettings.DistributionTime.Hours, 
                AppSettings.DistributionTime.Minutes);
        }

        private static DateTime GetNearestDate(DateTime now, int eventDayOfWeek, int eventHour, int eventMinutes)
        {
            var eventDate = now.ToMoscowTime().Date;
            var dayOfWeek = GetDayOfWeek(eventDate);

            while (eventDayOfWeek != dayOfWeek)
            {
                eventDate = eventDate.AddDays(1);
                dayOfWeek = GetDayOfWeek(eventDate);
            }

            eventDate = eventDate.AddHours(eventHour).AddMinutes(eventMinutes);
            return eventDate;
        }

        private static int GetDayOfWeek(DateTime date)
        {
            return date.DayOfWeek != 0 ? (int)date.DayOfWeek : 7;
        }
    }
}

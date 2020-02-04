using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
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
            _timer = new Timer(OnTimerElapsed, DateTime.UtcNow, Timeout.Infinite, Timeout.Infinite); 
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
            var now = (DateTime)state;

            await UpdateTotalPlayersMessagesAsync();

            if (DistributionTimeHasCome(now))
                await SendQuestionToAllUsersAsync();

            if (GameStarted(now))
                await ClearGameAttrsAsync();
        }

        private async Task ClearGameAttrsAsync()
        {
            try
            {
                await SheetController.GetInstance().ClearGameAttrsAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Excel-file updating error");
                await _messageController.SendTextMessageToBotOwnerAsync("Ошибка при обновлении excel-файла");
            }
        }

        private async Task UpdateTotalPlayersMessagesAsync()
        {
            try
            {
                await _messageController.UpdateTotalPlayersMessagesAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages");
                await _messageController.SendTextMessageToBotOwnerAsync($"Ошибка при обновлении сообщений с отметившимися игроками: {ex.Message}");
            }
        }

        private async Task SendQuestionToAllUsersAsync()
        {
            try
            {
                await _messageController.SendQuestionToAllUsersAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error on StartPlayersSetDeterminationAsync");
                await _messageController.SendTextMessageToBotOwnerAsync($"Ошибка при определении списка игроков: {ex.Message}");
            }
        }

        private bool DistributionTimeHasCome(DateTime now)
        {
            return GetDayOfWeek(now.ToMoscowTime()) == AppSettings.DistributionTime.Days
                && now.ToMoscowTime().TimeOfDay.Hours == AppSettings.DistributionTime.Hours
                && now.ToMoscowTime().TimeOfDay.Minutes == AppSettings.DistributionTime.Minutes;
        }

        private bool GameStarted(DateTime now)
        {
            var gameDate = GetNearestGameDateMoscowTime(now.ToMoscowTime());
            return now.ToMoscowTime().Year == gameDate.Year 
                && now.ToMoscowTime().Month == gameDate.Month 
                && now.ToMoscowTime().Day == gameDate.Day
                && now.ToMoscowTime().Hour == gameDate.Hour 
                && now.ToMoscowTime().Minute == gameDate.Minute;
        }
        
        public static DateTime GetNearestGameDateMoscowTime(DateTime startDate)
        {
            return GetNearestDate(startDate, 
                AppSettings.GameDay.Days, 
                AppSettings.GameDay.Hours, 
                AppSettings.GameDay.Minutes);
        }
        
        private static DateTime GetNearestDate(DateTime startDate, int eventDayOfWeek, int eventHour, int eventMinutes)
        {
            var eventDate = startDate.ToMoscowTime().Date;
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

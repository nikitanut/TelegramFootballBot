using Serilog;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Timers;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Controllers
{
    public class Scheduler
    {
        private readonly Timer _timer;
        private readonly MessageController _messageController;
        private readonly ILogger _logger;

        public Scheduler(MessageController messageController, ILogger logger)
        {
            _timer = new Timer(60 * 1000);
            _messageController = messageController;
            _logger = logger;
        }

        public void Run()
        {
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (DistributionTimeHasCome(e.SignalTime))
                await _messageController.StartPlayersSetDeterminationAsync();

            if (NeedToUpdateTotalPlayers(e.SignalTime))
                await _messageController.UpdateTotalPlayersMessagesAsync();

            if (GameStarted(e.SignalTime))
                await _messageController.ClearGameAttrsAsync();

            if (e.SignalTime.Minute == 0)
            {
                try { FileController.UpdatePlayers(Bot.Players); }
                catch (FileNotFoundException)
                {
                    _logger.Error("Players file not found");
                    _messageController.SendTextMessageToBotOwnerAsync("Файл с игроками не найден");
                }
                catch (SerializationException ex)
                {
                    _logger.Error(ex, "Serialization error");
                    _messageController.SendTextMessageToBotOwnerAsync("Ошибка сериализации");
                }
            }
        }

        private bool DistributionTimeHasCome(DateTime startDate)
        {
            var dayOfWeek = GetDayOfWeek(startDate);
            return dayOfWeek == AppSettings.DistributionTime.Days
                && startDate.TimeOfDay.Hours == AppSettings.DistributionTime.Hours
                && startDate.TimeOfDay.Minutes == AppSettings.DistributionTime.Minutes;
        }

        private bool NeedToUpdateTotalPlayers(DateTime startDate)
        {
            return startDate > GetDistributionDate() && startDate < GetGameDate(startDate);
        }

        private bool GameStarted(DateTime startDate)
        {
            var gameDate = GetGameDate(startDate);
            return startDate.Year == gameDate.Year 
                && startDate.Month == gameDate.Month 
                && startDate.Hour == gameDate.Hour 
                && startDate.Minute == gameDate.Minute;
        }

        private int GetDaysLeftBeforeGame()
        {
            var daysLeft = 0;
            var tempDate = DateTime.Now;

            var dayOfWeek = GetDayOfWeek(tempDate);

            while (AppSettings.GameDay.Days != dayOfWeek)
            {
                daysLeft++;
                tempDate = tempDate.AddDays(1);
                dayOfWeek = GetDayOfWeek(tempDate);
            }

            return daysLeft;
        }

        public static DateTime GetGameDate(DateTime startDate)
        {
            var gameDate = startDate.Date;
            var dayOfWeek = GetDayOfWeek(gameDate);

            while (AppSettings.GameDay.Days != dayOfWeek)
            {
                gameDate = gameDate.AddDays(1);
                dayOfWeek = GetDayOfWeek(gameDate);
            }

            gameDate = gameDate.AddHours(AppSettings.GameDay.Hours).AddMinutes(AppSettings.GameDay.Minutes);
            return gameDate > DateTime.Now ? gameDate : gameDate.AddDays(7);
        }

        private DateTime GetDistributionDate()
        {
            var distributionDate = DateTime.Now;
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

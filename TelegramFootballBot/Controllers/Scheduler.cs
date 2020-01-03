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

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (DistributionTimeHasCome(e.SignalTime))
                _messageController.StartPlayersSetDeterminationAsync();

            if (NeedToUpdateTotalPlayers(e.SignalTime))
                _messageController.StartUpdateTotalPlayersMessagesAsync();

            if (GameStarted(e.SignalTime))
            {
                try { _messageController.ClearGameAttrs(); }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Excel-file updating error");
                    _messageController.SendTextMessageToBotOwnerAsync("Ошибка при обновлении excel-файла");
                }
            }

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
            var dayOfWeek = startDate.DayOfWeek != 0 ? (int)startDate.DayOfWeek : 7;
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

            var dayOfWeek = tempDate.DayOfWeek != 0 ? (int)tempDate.DayOfWeek : 7;

            while (AppSettings.GameDay.Days != dayOfWeek)
            {
                daysLeft++;
                tempDate = tempDate.AddDays(1);
                dayOfWeek = tempDate.DayOfWeek != 0 ? (int)tempDate.DayOfWeek : 7;
            }

            return daysLeft;
        }

        public static DateTime GetGameDate(DateTime startDate)
        {
            var gameDate = startDate.Date;
            var dayOfWeek = gameDate.DayOfWeek != 0 ? (int)gameDate.DayOfWeek : 7;

            while (AppSettings.GameDay.Days != dayOfWeek)
            {
                gameDate = gameDate.AddDays(1);
                dayOfWeek = gameDate.DayOfWeek != 0 ? (int)gameDate.DayOfWeek : 7;
            }

            gameDate = gameDate.AddHours(AppSettings.GameDay.Hours).AddMinutes(AppSettings.GameDay.Minutes);
            return gameDate > DateTime.Now ? gameDate : gameDate.AddDays(1);
        }

        private DateTime GetDistributionDate()
        {
            var distributionDate = DateTime.Now;
            var dayOfWeek = distributionDate.DayOfWeek != 0 ? (int)distributionDate.DayOfWeek : 7;

            while (AppSettings.DistributionTime.Days != dayOfWeek)
            {
                distributionDate = distributionDate.AddDays(1);
                dayOfWeek = distributionDate.DayOfWeek != 0 ? (int)distributionDate.DayOfWeek : 7;
            }

            return distributionDate;
        }
    }
}

using System;
using System.Timers;

namespace TelegramFootballBot.Controllers
{
    public class Scheduler
    {
        private readonly Timer timer;
        private readonly MessageController MessageController;

        public Scheduler(MessageController MessageController)
        {
            this.MessageController = MessageController;
            timer = new Timer(60 * 1000);
        }

        public void Run()
        {
            timer.Elapsed += OnDistributionDateHasCome;
            timer.Start();
        }

        private void OnDistributionDateHasCome(object sender, ElapsedEventArgs e)
        {
            if (DistributionTimeHasCome(e.SignalTime))
            {
                var daysLeftBeforeGame = GetDaysLeftBeforeGame();
                MessageController.StartPlayersSetDeterminationAsync(daysLeftBeforeGame);
            }

            if (NeedToUpdateTotalPlayers(e.SignalTime))
                MessageController.StartUpdateTotalPlayersMessagesAsync();

            if (GameStarted(e.SignalTime))
                MessageController.ClearGameAttrs();
        }

        private bool DistributionTimeHasCome(DateTime dateTime)
        {
            var dayOfWeek = dateTime.DayOfWeek != 0 ? (int)dateTime.DayOfWeek : 7;
            return dayOfWeek == AppSettings.DistributionTime.Days
                && dateTime.TimeOfDay.Hours == AppSettings.DistributionTime.Hours
                && dateTime.TimeOfDay.Minutes == AppSettings.DistributionTime.Minutes;
        }

        private bool NeedToUpdateTotalPlayers(DateTime dateTime)
        {
            return dateTime > GetDistributionDate() && dateTime < GetGameDate();
        }

        private bool GameStarted(DateTime dateTime)
        {
            var gameDate = GetGameDate();
            return dateTime.Year == gameDate.Year 
                && dateTime.Month == gameDate.Month 
                && dateTime.Hour == gameDate.Hour 
                && dateTime.Minute == gameDate.Minute;
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

        private DateTime GetGameDate()
        {
            var gameDate = DateTime.Now;
            var dayOfWeek = gameDate.DayOfWeek != 0 ? (int)gameDate.DayOfWeek : 7;

            while (AppSettings.GameDay.Days != dayOfWeek)
            {
                gameDate = gameDate.AddDays(1);
                dayOfWeek = gameDate.DayOfWeek != 0 ? (int)gameDate.DayOfWeek : 7;
            }

            gameDate = gameDate.AddHours(AppSettings.GameDay.Hours).AddMinutes(AppSettings.GameDay.Minutes);
            return gameDate;
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

using System;
using System.Timers;

namespace TelegramFootballBot.Models.Processors
{
    public class Scheduler
    {
        private readonly Timer timer;
        private readonly MessageProcessor messageProcessor;

        public Scheduler(MessageProcessor messageProcessor)
        {
            this.messageProcessor = messageProcessor;
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
                messageProcessor.StartPlayersSetDetermination();
        }

        private bool DistributionTimeHasCome(DateTime dateTime)
        {
            if (dateTime == null) return false;

            // TODO: Change when implementation will be determined
            var dayOfWeek = dateTime.DayOfWeek != 0 ? (int)dateTime.DayOfWeek : 7;

            return dayOfWeek == AppSettings.DistributionTime.Days
                && dateTime.TimeOfDay.Hours == AppSettings.DistributionTime.Hours
                && dateTime.TimeOfDay.Minutes == AppSettings.DistributionTime.Minutes;
        }
    }
}

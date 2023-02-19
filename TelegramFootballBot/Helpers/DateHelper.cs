using System;
using System.Collections.Generic;

namespace TelegramFootballBot.Core.Helpers
{
    public static class DateHelper
    {
        private static readonly Dictionary<int, string> _russianMonthNames = new() { { 1, "января" }, { 2, "февраля" }, { 3, "марта" }, { 4, "апреля" }, { 5, "мая" }, { 6, "июня" }, { 7, "июля" }, { 8, "августа" }, { 9, "сентября" }, { 10, "октября" }, { 11, "ноября" }, { 12, "декабря" } };

        public static bool DistributionTimeHasCome(DateTime currentDate)
        {
            var distributionDate = GetNearestDistributionDateMoscowTime(currentDate);
            return GetDayOfWeek(currentDate.ToMoscowTime()) == GetDayOfWeek(distributionDate)
                && currentDate.ToMoscowTime().TimeOfDay.Hours == distributionDate.TimeOfDay.Hours
                && currentDate.ToMoscowTime().TimeOfDay.Minutes == distributionDate.TimeOfDay.Minutes;
        }

        public static bool TeamsGenerationTimeHasCome(DateTime currentDate)
        {
            var gameDate = GetNearestGameDateMoscowTime(currentDate);
            return currentDate.ToMoscowTime().Year == gameDate.Year
                && currentDate.ToMoscowTime().Month == gameDate.Month
                && currentDate.ToMoscowTime().Day == gameDate.Day
                && currentDate.ToMoscowTime().Hour == gameDate.Hour - 1 // 1 hour before game
                && currentDate.ToMoscowTime().Minute == gameDate.Minute;
        }

        public static bool GameStarted(DateTime currentDate)
        {
            var gameDate = GetNearestGameDateMoscowTime(currentDate);
            return currentDate.ToMoscowTime().Year == gameDate.Year
                && currentDate.ToMoscowTime().Month == gameDate.Month
                && currentDate.ToMoscowTime().Day == gameDate.Day
                && currentDate.ToMoscowTime().Hour == gameDate.Hour
                && currentDate.ToMoscowTime().Minute == gameDate.Minute;
        }

        public static DateTime GetNearestGameDateMoscowTime(DateTime currentDate)
        {
            return GetNearestDate(currentDate,
                AppSettings.GameDay.Days,
                AppSettings.GameDay.Hours,
                AppSettings.GameDay.Minutes);
        }


        public static DateTime ToMoscowTime(this DateTime dateTime)
        {
            return dateTime.ToUniversalTime().AddHours(Constants.MOSCOW_UTC_OFFSET);
        }

        public static string ToRussianDayMonthString(this DateTime dateTime)
        {
            return $"{dateTime.Day} {_russianMonthNames[dateTime.Month]}";
        }

        public static DateTime GetNearestDistributionDateMoscowTime(DateTime currentDate)
        {
            return GetNearestDate(currentDate,
                AppSettings.DistributionTime.Days,
                AppSettings.DistributionTime.Hours,
                AppSettings.DistributionTime.Minutes);
        }

        private static DateTime GetNearestDate(DateTime currentDate, int eventDayOfWeek, int eventHour, int eventMinutes)
        {
            if (eventDayOfWeek > 7)
                throw new ArgumentOutOfRangeException(nameof(eventDayOfWeek));

            var eventDate = currentDate.ToMoscowTime().Date;
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

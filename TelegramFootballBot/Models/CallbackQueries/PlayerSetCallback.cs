using System;
using System.Linq;
using TelegramFootballBot.Core.Helpers;

namespace TelegramFootballBot.Core.Models.CallbackQueries
{
    public class PlayerSetCallback : Callback
    {
        public static string Name => "PlayersSetDetermination";

        public DateTime GameDate { get; private set; }

        public PlayerSetCallback(string callbackData) : base(callbackData)
        {            
            GameDate = GetGameDate(callbackData);
        }

        /// <summary>
        /// Get text for player set callback prefix
        /// </summary>
        /// <param name="gameDate">Date of game</param>
        /// <returns>Callback data text</returns>
        public static string GetCallbackPrefix(DateTime gameDate)
        {
            return Name + Constants.CALLBACK_DATA_SEPARATOR + gameDate.ToString("yyyy-MM-dd");
        }

        private static DateTime GetGameDate(string callbackData)
        {
            var gameDateString = Prefix(callbackData).Split(Constants.CALLBACK_DATA_SEPARATOR).Last();
            if (!DateTime.TryParse(gameDateString, out DateTime gameDate))
                throw new ArgumentException($"Game date was not provided for callback data: {callbackData}", nameof(callbackData));
            return gameDate;
        }
    }
}

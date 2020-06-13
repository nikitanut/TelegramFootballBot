using System;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramFootballBot.Helpers
{
    public static class MarkupHelper
    {
        /// <summary>
        /// Gets custom InlineKeyboardMarkup with buttons
        /// </summary>
        /// <param name="callbackPrefix">Callback prefix for message</param>
        /// <param name="buttonsLabels">Array of buttons texts</param>
        public static InlineKeyboardMarkup GetKeyBoardMarkup(string callbackPrefix, params string [] buttonsLabels)
        {
            var keyBoard = new[] { new InlineKeyboardButton[buttonsLabels.Length] };

            for (var i = 0; i < keyBoard[0].Length; i++)
            {
                keyBoard[0][i] = new InlineKeyboardButton
                {
                    Text = buttonsLabels[i],
                    CallbackData = $"{callbackPrefix}{Constants.CALLBACK_DATA_SEPARATOR}{buttonsLabels[i]}"
                };
            }

            return new InlineKeyboardMarkup(keyBoard);
        }

        /// <summary>
        /// Gets markup for determination question
        /// </summary>
        /// <param name="gameDate">Date of game</param>
        /// <returns></returns>
        public static InlineKeyboardMarkup GetUserDeterminationMarkup(DateTime gameDate)
        {
            return GetKeyBoardMarkup(GetGameStartCallbackPrefix(gameDate), Constants.YES_ANSWER, Constants.NO_ANSWER, Constants.MAYBE_ANSWER);
        }

        public static string GetDashedString()
        {
            return new string('-', 30);
        }

        /// <summary>
        /// Get prefix for game start callback
        /// </summary>
        /// <param name="gameDate">Date of game</param>
        /// <returns></returns>
        private static string GetGameStartCallbackPrefix(DateTime gameDate)
        {
            return Constants.PLAYERS_SET_CALLBACK_PREFIX
                 + Constants.PLAYERS_SET_CALLBACK_PREFIX_SEPARATOR
                 + gameDate.ToString("yyyy-MM-dd");
        }
    }
}

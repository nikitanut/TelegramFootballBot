using System;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramFootballBot.Helpers
{
    public static class MarkupHelper
    {
        /// <summary>
        /// Gets custom InlineKeyboardMarkup with buttons
        /// </summary>
        /// <param name="buttonsTexts">Array of buttons texts</param>
        public static InlineKeyboardMarkup GetKeyBoardMarkup(string callbackPrefix, params string [] buttonsTexts)
        {
            var keyBoard = new[] { new InlineKeyboardButton[buttonsTexts.Length] };

            for (var i = 0; i < keyBoard[0].Length; i++)
            {
                keyBoard[0][i] = new InlineKeyboardButton
                {
                    Text = buttonsTexts[i],
                    CallbackData = $"{callbackPrefix}{Constants.CALLBACK_DATA_SEPARATOR}{buttonsTexts[i]}"
                };
            }

            return new InlineKeyboardMarkup(keyBoard);
        }

        public static InlineKeyboardMarkup GetUserDeterminationMarkup(DateTime gameDate)
        {
            return GetKeyBoardMarkup(GetGameStartCallbackPrefix(gameDate), Constants.YES_ANSWER, Constants.NO_ANSWER, Constants.MAYBE_ANSWER);
        }

        private static string GetGameStartCallbackPrefix(DateTime gameDate)
        {
            return Constants.PLAYERS_SET_CALLBACK_PREFIX
                 + Constants.PLAYERS_SET_CALLBACK_PREFIX_SEPARATOR
                 + gameDate.ToString("yyyy-MM-dd");
        }
    }
}

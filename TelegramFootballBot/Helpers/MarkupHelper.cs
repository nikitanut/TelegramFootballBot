using System;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Models.CallbackQueries;

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
                    CallbackData = Callback.ToCallbackText(callbackPrefix, buttonsLabels[i])
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
            return GetKeyBoardMarkup(PlayerSetCallback.GetCallbackPrefix(gameDate), Constants.YES_ANSWER, Constants.NO_ANSWER, Constants.MAYBE_ANSWER);
        }

        public static InlineKeyboardMarkup GetTeamPollMarkup(Guid activePollId)
        {
            return GetKeyBoardMarkup(TeamPollCallback.GetCallbackPrefix(activePollId), Constants.YES_ANSWER, Constants.NO_ANSWER);
        }

        public static string GetDashedString()
        {
            return new string('-', 30);
        }
    }
}

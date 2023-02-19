using System;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Core.Models.CallbackQueries;

namespace TelegramFootballBot.Core.Helpers
{
    public static class MarkupHelper
    {
        public static InlineKeyboardMarkup GetKeyBoardMarkup(string callbackPrefix, string [] buttonsLabels, string [] buttonValues)
        {
            var keyBoard = new[] { new InlineKeyboardButton[buttonsLabels.Length] };

            for (var i = 0; i < keyBoard[0].Length; i++)
            {
                keyBoard[0][i] = new InlineKeyboardButton(buttonsLabels[i])
                {                  
                    CallbackData = Callback.ToCallbackText(callbackPrefix, buttonValues[i])
                };
            }

            return new InlineKeyboardMarkup(keyBoard);
        }

        public static InlineKeyboardMarkup GetUserReadyToPlayQuestion(DateTime gameDate)
        {
            var labels = new[] { Constants.YES_ANSWER, Constants.NO_ANSWER, Constants.MAYBE_ANSWER };
            var values = labels;
            return GetKeyBoardMarkup(PlayerSetCallback.GetCallbackPrefix(gameDate), labels, values);
        }

        public static InlineKeyboardMarkup GetTeamPollMarkup(Guid activePollId)
        {
            var labels = new[] { Constants.LIKE_EMOJI, Constants.DISLIKE_EMOJI };
            var values = new [] { Constants.YES_ANSWER, Constants.NO_ANSWER };
            return GetKeyBoardMarkup(TeamPollCallback.GetCallbackPrefix(activePollId), labels, values);
        }

        public static string DashedString => new('-', 30);
    }
}

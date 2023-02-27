using System;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Core.Models.CallbackQueries;

namespace TelegramFootballBot.Core.Helpers
{
    public static class MarkupHelper
    {
        public static InlineKeyboardMarkup GetIfReadyToPlayQuestion(DateTime gameDate)
        {
            var labels = new[] { Constants.YES_ANSWER, Constants.NO_ANSWER, Constants.MAYBE_ANSWER };
            return GetKeyBoardMarkup(PlayerSetCallback.BuildCallbackPrefix(gameDate), labels, labels);
        }

        private static InlineKeyboardMarkup GetKeyBoardMarkup(string callbackPrefix, string [] buttonsLabels, string [] buttonValues)
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

        public static string DashedString => new('-', 30);
    }
}

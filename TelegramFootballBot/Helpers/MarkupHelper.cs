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
    }
}

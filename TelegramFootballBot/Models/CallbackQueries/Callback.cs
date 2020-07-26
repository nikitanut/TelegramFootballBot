using System;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.CallbackQueries
{
    public abstract class Callback
    {
        public string UserAnswer { get; private set; }

        public Callback(string callbackData)
        {
            Validate(callbackData);
            UserAnswer = GetUserAnswer(callbackData);
        }
        
        public static string ToCallbackText(string callbackPrefix, string text)
        {
            return $"{callbackPrefix}{Constants.CALLBACK_PREFIX_SEPARATOR}{text}";
        }

        private static void Validate(string callbackData)
        {
            if (string.IsNullOrEmpty(callbackData) || !callbackData.Contains(Constants.CALLBACK_PREFIX_SEPARATOR))
                throw new ArgumentException($"Prefix was not provided for callback data: {callbackData}");
        }

        private string GetUserAnswer(string callbackData)
        {
            return callbackData.Split(Constants.CALLBACK_PREFIX_SEPARATOR)[1];
        }
                        
        public static string GetCallbackName(string callbackData)
        {
            Validate(callbackData);
            return Prefix(callbackData).Split(Constants.CALLBACK_PREFIX_DATA_SEPARATOR)[0];
        }

        protected static string Prefix(string callbackData)
        {
            return callbackData.Split(Constants.CALLBACK_PREFIX_SEPARATOR)[0];
        }
    }
}

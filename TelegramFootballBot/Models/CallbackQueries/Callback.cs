using System;
using System.Linq;
using TelegramFootballBot.Core.Helpers;

namespace TelegramFootballBot.Core.Models.CallbackQueries
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

        private static string GetUserAnswer(string callbackData)
        {
            return callbackData.Split(Constants.CALLBACK_PREFIX_SEPARATOR).Last();
        }
                        
        public static string GetCallbackName(string callbackData)
        {
            Validate(callbackData);
            return Prefix(callbackData).Split(Constants.CALLBACK_DATA_SEPARATOR).First();
        }

        protected static string Prefix(string callbackData)
        {
            return callbackData.Split(Constants.CALLBACK_PREFIX_SEPARATOR).First();
        }
    }
}

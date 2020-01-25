using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramFootballBot.Helpers
{
    public static class ExtensionMethods
    {
        public static async Task<Message> SendTextMessageWithTokenAsync(this TelegramBotClient client, ChatId chatId, string text, IReplyMarkup replyMarkup = null)
        {
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            return await client.SendTextMessageAsync(chatId, text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }

        public static async Task<Message> EditMessageTextWithTokenAsync(this TelegramBotClient client, ChatId chatId, int messageId, string text)
        {
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            return await client.EditMessageTextAsync(chatId, messageId, text, cancellationToken: cancellationToken);
        }

        public static async Task<Message> SendTextMessageToBotOwnerAsync(this TelegramBotClient client, string text)
        {
            try
            {
                var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                return await client.SendTextMessageAsync(AppSettings.BotOwnerChatId, text, cancellationToken: cancellationToken);
            }
            catch
            {
                // TODO: Log
                return new Message();
            }
        }

        public static async Task<Message> SendErrorMessageToUser(this TelegramBotClient client, ChatId chatId, string playerName)
        {
            try
            {
                return await client.SendTextMessageWithTokenAsync(chatId, $"Неизвестная ошибка");
            }
            catch (Exception ex)
            {
                return await client.SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
            }
        }

        public static DateTime ToMoscowTime(this DateTime dateTime)
        {
            var moscowUtcOffset = 3;
            return dateTime.ToUniversalTime().AddHours(moscowUtcOffset);
        }
    }
}

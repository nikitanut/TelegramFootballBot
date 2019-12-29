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
    }
}

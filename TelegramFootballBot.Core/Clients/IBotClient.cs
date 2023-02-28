using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramFootballBot.Core.Clients
{
    /// <summary>
    /// Wrapper for ITelegramBotClient as its methods are extension and can't be tested
    /// </summary>
    public interface IBotClient
    {
        Task<Message> SendTextMessageAsync(ChatId chatId, string text, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default);

        Task<Message> EditMessageTextAsync(ChatId chatId, int messageId, string text, InlineKeyboardMarkup replyMarkup = null, CancellationToken cancellationToken = default);

        Task<Message> EditMessageReplyMarkupAsync(ChatId chatId, int messageId, InlineKeyboardMarkup replyMarkup = null, CancellationToken cancellationToken = default);

        Task DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default);

    }
}

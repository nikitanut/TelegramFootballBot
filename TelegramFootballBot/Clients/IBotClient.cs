using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramFootballBot.Core.Clients
{
    /// <summary>
    /// Wrapper for ITelegramBotClient as its methods are extension and can't be tested
    /// </summary>
    public interface IBotClient
    {
        Task<Message> SendTextMessageAsync(ChatId chatId, string text, ParseMode? parseMode = null, IEnumerable<MessageEntity> entities = null, bool? disableWebPagePreview = null, bool? disableNotification = null, bool? protectContent = null, int? replyToMessageId = null, bool? allowSendingWithoutReply = null, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default);

        Task<Message> EditMessageTextAsync(ChatId chatId, int messageId, string text, ParseMode? parseMode = null, IEnumerable<MessageEntity> entities = null, bool? disableWebPagePreview = null, InlineKeyboardMarkup replyMarkup = null, CancellationToken cancellationToken = default);

        Task<Message> EditMessageReplyMarkupAsync(ChatId chatId, int messageId, InlineKeyboardMarkup replyMarkup = null, CancellationToken cancellationToken = default);

        Task DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default);

    }
}

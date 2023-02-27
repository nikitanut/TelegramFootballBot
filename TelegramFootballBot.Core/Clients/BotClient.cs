using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramFootballBot.Core.Clients
{
    public class BotClient : IBotClient
    {
        private readonly ITelegramBotClient _telegramBotClient;

        /// <summary>
        /// Wrapper for ITelegramBotClient as its methods are extension and can't be tested
        /// </summary>
        /// <param name="telegramBotClient">Telegram bot client</param>
        public BotClient(ITelegramBotClient telegramBotClient)
        {
            _telegramBotClient = telegramBotClient;
        }

        public async Task<Message> SendTextMessageAsync(ChatId chatId, string text, ParseMode? parseMode = null, IEnumerable<MessageEntity> entities = null, bool? disableWebPagePreview = null, bool? disableNotification = null, bool? protectContent = null, int? replyToMessageId = null, bool? allowSendingWithoutReply = null, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default)
        {
            return await _telegramBotClient.SendTextMessageAsync(chatId, text, parseMode, entities, disableWebPagePreview, disableNotification, protectContent, replyToMessageId, allowSendingWithoutReply, replyMarkup, cancellationToken);
        }

        public async Task<Message> EditMessageTextAsync(ChatId chatId, int messageId, string text, ParseMode? parseMode = null, IEnumerable<MessageEntity> entities = null, bool? disableWebPagePreview = null, InlineKeyboardMarkup replyMarkup = null, CancellationToken cancellationToken = default)
        {
            return await _telegramBotClient.EditMessageTextAsync(chatId, messageId, text, parseMode, entities, disableWebPagePreview, replyMarkup, cancellationToken);
        }

        public async Task<Message> EditMessageReplyMarkupAsync(ChatId chatId, int messageId, InlineKeyboardMarkup replyMarkup = null, CancellationToken cancellationToken = default)
        {
            return await _telegramBotClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup, cancellationToken);
        }

        public async Task DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
        {
            await _telegramBotClient.DeleteMessageAsync(chatId, messageId, cancellationToken);
        }
    }
}

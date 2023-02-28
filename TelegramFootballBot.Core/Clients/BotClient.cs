using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
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

        public async Task<Message> SendTextMessageAsync(ChatId chatId, string text, IReplyMarkup replyMarkup = null, CancellationToken cancellationToken = default)
        {
            return await _telegramBotClient.SendTextMessageAsync(chatId, text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
        }

        public async Task<Message> EditMessageTextAsync(ChatId chatId, int messageId, string text, InlineKeyboardMarkup replyMarkup = null, CancellationToken cancellationToken = default)
        {
            return await _telegramBotClient.EditMessageTextAsync(chatId, messageId, text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
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

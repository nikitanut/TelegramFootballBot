using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Core.Models;

namespace TelegramFootballBot.Core.Services
{
    public interface IMessageService
    {
        Task<List<SendMessageResponse>> SendMessagesAsync(string text, IEnumerable<ChatId> chats, IReplyMarkup replyMarkup = null);

        Task<List<SendMessageResponse>> EditMessagesAsync(string text, IEnumerable<Message> messagesToRefresh);

        Task<Message> SendMessageToBotOwnerAsync(string text, IReplyMarkup replyMarkup = null);

        Task<Message> SendMessageAsync(ChatId chatId, string text, IReplyMarkup replyMarkup = null);

        Task DeleteMessageAsync(ChatId chatId, int messageId);

        Task<Message> SendErrorMessageToUserAsync(ChatId chatId, string playerName);

        Task ClearReplyMarkupAsync(ChatId chatId, int messageId);
    }
}

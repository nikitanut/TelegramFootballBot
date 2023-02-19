using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramFootballBot.Core.Services
{
    public interface IMessageService
    {
        Task SendMessageToAllPlayersAsync(string text, IReplyMarkup replyMarkup = null);

        Task RefreshTotalPlayersMessageAsync();

        Task RefreshPollMessageAsync();

        Task SendGeneratedTeamsMessageAsync();

        Task<Message> SendMessageToBotOwnerAsync(string text, IReplyMarkup replyMarkup = null);

        Task<Message> SendMessageAsync(ChatId chatId, string text, IReplyMarkup replyMarkup = null);

        Task DeleteMessageAsync(ChatId chatId, int messageId);

        Task<Message> SendErrorMessageToUserAsync(ChatId chatId, string playerName);

        Task<Message> EditMessageAsync(ChatId chatId, int messageId, string text);

        Task ClearReplyMarkupAsync(ChatId chatId, int messageId);
    }
}

using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramFootballBot.Core.Services
{
    public interface IMessageService
    {
        Task SendMessageToAllUsersAsync(string text, IReplyMarkup replyMarkup = null);

        Task SendDistributionQuestionAsync();

        Task UpdateTotalPlayersMessagesAsync();

        Task UpdatePollMessagesAsync();

        Task SendTeamPollMessageAsync();

        Task<Message> SendTextMessageToBotOwnerAsync(string text, IReplyMarkup replyMarkup = null);

        Task<Message> SendMessageAsync(ChatId chatId, string text, IReplyMarkup replyMarkup = null);

        Task DeleteMessageAsync(ChatId chatId, int messageId);

        Task<Message> SendErrorMessageToUserAsync(ChatId chatId, string playerName);

        Task<Message> EditMessageTextAsync(ChatId chatId, int messageId, string text);

        Task ClearReplyMarkupAsync(ChatId chatId, int messageId);
    }
}

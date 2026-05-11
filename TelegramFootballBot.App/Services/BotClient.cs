using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramFootballBot.App.Services;

/// <summary>
/// Wrapper for ITelegramBotClient as its methods are extension and can't be tested
/// </summary>
/// <param name="telegramBotClient">Telegram bot client</param>
public class BotClient(ITelegramBotClient telegramBotClient) : IBotClient
{
    public async Task<Message> SendTextMessageAsync(ChatId chatId, string text, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        return await telegramBotClient.SendMessage(chatId, text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
    }

    public async Task<Message> EditMessageTextAsync(ChatId chatId, int messageId, string text, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        return await telegramBotClient.EditMessageText(chatId, messageId, text, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
    }

    public async Task<Message> EditMessageReplyMarkupAsync(ChatId chatId, int messageId, InlineKeyboardMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        return await telegramBotClient.EditMessageReplyMarkup(chatId, messageId, replyMarkup, cancellationToken: cancellationToken);
    }

    public async Task DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
    {
        await telegramBotClient.DeleteMessage(chatId, messageId, cancellationToken);
    }
}

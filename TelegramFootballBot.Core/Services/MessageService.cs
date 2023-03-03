using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Core.Clients;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core.Models;

namespace TelegramFootballBot.Core.Services
{
    public class MessageService : IMessageService
    {
        private readonly IBotClient _botClient;
        private readonly ILogger _logger;

        public MessageService(IBotClient botClient, ILogger logger)
        {
            _botClient = botClient;
            _logger = logger;            
        }

        public async Task<List<SendMessageResponse>> SendMessagesAsync(string text, IEnumerable<ChatId> chats, IReplyMarkup replyMarkup = null)
        {
            var requests = chats.ToDictionary(chatId => SendMessageAsync(chatId, text, replyMarkup), chatId => chatId);
            return await ExecuteRequests(requests);
        }

        public async Task<List<SendMessageResponse>> EditMessagesAsync(string text, IEnumerable<Message> messagesToEdit)
        {
            var requests = messagesToEdit.ToDictionary(m => EditMessageAsync(m, text), m => (ChatId)m.Chat.Id);
            return await ExecuteRequests(requests);
        }

        private async Task<Message> EditMessageAsync(Message message, string text)
        {
            if (message.Text == text)
                return message;

            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            return await _botClient.EditMessageTextAsync(message.Chat.Id, message.MessageId, text, cancellationToken: cts.Token);
        }

        public async Task<Message> SendMessageToBotOwnerAsync(string text, IReplyMarkup replyMarkup = null)
        {
            if (AppSettings.NotifyOwner)
            {
                try
                {
                    return await SendMessageAsync(AppSettings.BotOwnerChatId, text, replyMarkup: replyMarkup);
                }
                catch (Exception ex)
                {
                    _logger.Error("An error occured while sending a message to the bot owner", ex);
                }
            }

            return new Message();
        }

        public async Task<Message> SendMessageAsync(ChatId chatId, string text, IReplyMarkup replyMarkup = null)
        {
            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            return await _botClient.SendTextMessageAsync(chatId, text, replyMarkup: replyMarkup, cancellationToken: cts.Token);
        }

        public async Task DeleteMessageAsync(ChatId chatId, int messageId)
        {
            try
            {
                using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
                await _botClient.DeleteMessageAsync(chatId, messageId, cancellationToken: cts.Token);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while deleting message");
            }
        }

        public async Task<Message> SendErrorMessageToUserAsync(ChatId chatId, string playerName)
        {
            try
            {
                return await SendMessageAsync(chatId, "Неизвестная ошибка");
            }
            catch (Exception ex)
            {
                return await SendMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
            }
        }

        private static async Task<List<SendMessageResponse>> ExecuteRequests(Dictionary<Task<Message>, ChatId> requests)
        {
            var responses = new List<SendMessageResponse>();
            var requestsProcessing = new Dictionary<Task<Message>, ChatId>(requests);

            while (requestsProcessing.Any())
            {
                var response = await Task.WhenAny(requestsProcessing.Keys);
                var chatId = requestsProcessing[response];
                requestsProcessing.Remove(response);
                var errorMessage = string.Empty;

                if (response.IsFaulted || response.IsCanceled)
                    errorMessage = response.IsFaulted ? response.Exception.Message : $"Timeout {Constants.ASYNC_OPERATION_TIMEOUT} ms";

                responses.Add(new SendMessageResponse
                {
                    ChatId = chatId,
                    Status = errorMessage == string.Empty ? SendStatus.Success : SendStatus.Error,
                    Message = errorMessage
                });
            }

            return responses;
        }

        public async Task ClearReplyMarkupAsync(ChatId chatId, int messageId)
        {
            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            await _botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { Array.Empty<InlineKeyboardButton>() }, cancellationToken: cts.Token);
        }
    }
}

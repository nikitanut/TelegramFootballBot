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
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, ChatId>();

            foreach (var chatId in chats)
            {
                var request = SendMessageAsync(chatId, text, replyMarkup);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, chatId);
            }

            return await ExecuteRequests(requests, playersRequestsIds);
        }

        public async Task<List<SendMessageResponse>> EditMessagesAsync(string text, IEnumerable<Message> messagesToEdit)
        {
            var requests = new List<Task<Message>>();
            var chatsRequestsIds = new Dictionary<int, ChatId>();

            foreach (var message in messagesToEdit)
            {
                var request = EditMessageAsync(message, text);
                requests.Add(request);
                chatsRequestsIds.Add(request.Id, message.Chat.Id);
            }

            return await ExecuteRequests(requests, chatsRequestsIds);
        }

        public async Task<Message> EditMessageAsync(ChatId chatId, int messageId, string text)
        {
            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            return await _botClient.EditMessageTextAsync(chatId, messageId, text, cancellationToken: cts.Token);
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
                _logger.Error(ex, $"Error on deleting message");
            }
        }

        public async Task<Message> SendErrorMessageToUserAsync(ChatId chatId, string playerName)
        {
            try
            {
                return await SendMessageAsync(chatId, $"Неизвестная ошибка");
            }
            catch (Exception ex)
            {
                return await SendMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
            }
        }

        private async Task<List<SendMessageResponse>> ExecuteRequests(List<Task<Message>> requests, Dictionary<int, ChatId> chatsRequestsIds)
        {
            var responses = new List<SendMessageResponse>();

            while (requests.Any())
            {
                var response = await Task.WhenAny(requests);
                requests.Remove(response);
                var chatId = chatsRequestsIds.First(r => r.Key == response.Id).Value;
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

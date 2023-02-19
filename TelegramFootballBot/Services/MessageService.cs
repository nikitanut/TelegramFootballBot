using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core.Models;

namespace TelegramFootballBot.Core.Services
{
    public class MessageService : IMessageService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IPlayerRepository _playerRepository;
        private readonly ITeamService _teamService;
        private readonly ISheetService _sheetService;
        private readonly ILogger _logger;

        private string _approvedPlayersMessage = null;
        private string _messageWithLikes = null;

        public MessageService(ITelegramBotClient botClient, IPlayerRepository playerRepository, ITeamService teamsService, ISheetService sheetService, ILogger logger)
        {
            _botClient = botClient;
            _playerRepository = playerRepository;
            _teamService = teamsService;
            _sheetService = sheetService;
            _logger = logger;            
        }

        public async Task SendMessageToAllPlayersAsync(string text, IReplyMarkup replyMarkup = null)
        {
            var players = await _playerRepository.GetAllAsync();
            await SendMessageToPlayersAsync(players, text, replyMarkup);
        }

        public async Task RefreshTotalPlayersMessageAsync()
        {
            var approvedPlayersMessage = await _sheetService.BuildApprovedPlayersMessageAsync();
            if (approvedPlayersMessage == _approvedPlayersMessage)
                return;

            _approvedPlayersMessage = approvedPlayersMessage;
            var playersReceivedMessage = await _playerRepository.GetRecievedMessageAsync();
            await EditMessageAsync(playersReceivedMessage, _approvedPlayersMessage, Constants.APPROVED_PLAYERS_MESSAGE_TYPE);
        }

        public async Task RefreshPollMessageAsync()
        {
            var messageWithLikes = _teamService.GetMessageWithLikes();
            if (messageWithLikes == _messageWithLikes)
                return;

            _messageWithLikes = messageWithLikes;
            var playersVoted = await _playerRepository.GetVotedAsync();
            await EditMessageAsync(playersVoted, _messageWithLikes, Constants.TEAM_POLL_MESSAGE_TYPE);
        }

        public async Task SendGeneratedTeamsMessageAsync()
        {
            var pollMessage = _teamService.BuildMessageWithGeneratedTeams();
            if (string.IsNullOrEmpty(pollMessage))
                return;

            _messageWithLikes = _teamService.GetMessageWithLikes();
            var players = await _playerRepository.GetReadyToPlayAsync();
            await SendMessageToPlayersAsync(players, pollMessage, MarkupHelper.GetTeamPollMarkup(_teamService.GetActivePollId()));
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

        private async Task SendMessageToPlayersAsync(IEnumerable<Player> players, string text, IReplyMarkup replyMarkup = null)
        {
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();

            foreach (var player in players)
            {
                var request = SendMessageAsync(player.ChatId, text, replyMarkup);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ExecuteRequests(requests, playersRequestsIds);
        }

        private async Task EditMessageAsync(IEnumerable<Player> players, string text, string messageType)
        {
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();

            foreach (var player in players)
            {
                var request = EditMessageAsync(player.ChatId, MessageId(messageType, player), text);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ExecuteRequests(requests, playersRequestsIds);
        }

        private static int MessageId(string messageType, Player player)
        {
            return messageType switch
            {
                Constants.APPROVED_PLAYERS_MESSAGE_TYPE => player.ApprovedPlayersMessageId,
                Constants.TEAM_POLL_MESSAGE_TYPE => player.PollMessageId,
                _ => throw new ArgumentOutOfRangeException(nameof(messageType)),
            };
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

        private async Task ExecuteRequests(List<Task<Message>> requests, Dictionary<int, Player> playersRequestsIds)
        {
            while (requests.Any())
            {
                var response = await Task.WhenAny(requests);
                requests.Remove(response);                

                if (response.IsFaulted || response.IsCanceled)
                {
                    var errorMessage = response.IsFaulted ? response.Exception.Message : $"Тайм-аут {Constants.ASYNC_OPERATION_TIMEOUT} мс";
                    var player = playersRequestsIds.First(r => r.Key == response.Id).Value;
                    _logger.Error($"Error for user {player.Name}: {errorMessage}");
                    return;
                }
            }
        }

        public async Task<Message> EditMessageAsync(ChatId chatId, int messageId, string text)
        {
            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            return await _botClient.EditMessageTextAsync(chatId, messageId, text, cancellationToken: cts.Token);
        }

        public async Task ClearReplyMarkupAsync(ChatId chatId, int messageId)
        {
            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            await _botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { Array.Empty<InlineKeyboardButton>() }, cancellationToken: cts.Token);
        }
    }
}

using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Data;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Services
{
    public class MessageService : IMessageService
    {
        private readonly IPlayerRepository _playerRepository;

        private readonly ILogger _logger;
        private readonly ITelegramBotClient _client;
        private readonly TeamsService _teamsService;
        private string _approvedPlayersMessage = null;
        private string _likesMessage = null;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);

        public MessageService(ITelegramBotClient botClient, IPlayerRepository playerRepository, TeamsService teamsService, ILogger logger)
        {
            _client = botClient;
            _playerRepository = playerRepository;
            _teamsService = teamsService;
            _logger = logger;            
        }

        public async Task SendMessageToAllUsersAsync(string text, IReplyMarkup replyMarkup = null)
        {
            await SendMessageAsync(await _playerRepository.GetAllAsync(), text, replyMarkup);
        }

        public async Task SendDistributionQuestionAsync()
        {
            var gameDate = DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow);
            var message = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";
            var markup = MarkupHelper.GetUserDeterminationMarkup(gameDate);
            await SendMessageToAllUsersAsync(message, markup);
        }

        public async Task UpdateTotalPlayersMessagesAsync()
        {
            var approvedPlayersMessage = await SheetService.GetInstance().GetApprovedPlayersMessageAsync();
            if (approvedPlayersMessage == _approvedPlayersMessage)
                return;

            _approvedPlayersMessage = approvedPlayersMessage;
            await EditMessageAsync(await _playerRepository.GetRecievedMessageAsync(), _approvedPlayersMessage, Constants.APPROVED_PLAYERS_MESSAGE_TYPE);
        }

        public async Task UpdatePollMessagesAsync()
        {
            var likesMessage = _teamsService.LikesMessage();
            if (likesMessage == _likesMessage)
                return;

            _likesMessage = likesMessage;
            var playersVoted = (await _playerRepository.GetAllAsync()).Where(p => p.PollMessageId != 0);
            await EditMessageAsync(playersVoted, _likesMessage, Constants.TEAM_POLL_MESSAGE_TYPE);
        }

        public async Task SendTeamPollMessageAsync()
        {
            var pollMessage = _teamsService.GenerateMessage();
            if (string.IsNullOrEmpty(pollMessage))
                return;

            _likesMessage = _teamsService.LikesMessage();
            var players = await _playerRepository.GetReadyToPlayAsync();
            await SendMessageAsync(players, pollMessage, MarkupHelper.GetTeamPollMarkup(_teamsService.GetActivePollId()));
        }

        public async Task<Message> SendTextMessageToBotOwnerAsync(string text, IReplyMarkup replyMarkup = null)
        {
            if (AppSettings.NotifyOwner)
            {
                try
                {
                    return await SendMessageAsync(AppSettings.BotOwnerChatId, text, replyMarkup: replyMarkup);
                }
                catch
                {
                }
            }

            return new Message();
        }

        public async Task<Message> SendMessageAsync(ChatId chatId, string text, IReplyMarkup replyMarkup = null)
        {
            return await _client.SendTextMessageAsync(chatId, text, replyMarkup: replyMarkup, cancellationToken: _cts.Token);
        }

        private async Task SendMessageAsync(IEnumerable<Player> players, string text, IReplyMarkup replyMarkup = null, Action<Player, Message> actionOnSuccess = null)
        {
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();

            foreach (var player in players)
            {
                var request = SendMessageAsync(player.ChatId, text, replyMarkup);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ProcessRequests(requests, playersRequestsIds, actionOnSuccess);
        }

        private async Task EditMessageAsync(IEnumerable<Player> players, string text, string messageType)
        {
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();

            foreach (var player in players)
            {
                var request = EditMessageTextAsync(player.ChatId, MessageId(messageType, player), text);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ProcessRequests(requests, playersRequestsIds);
        }

        private int MessageId(string messageType, Player player)
        {
            switch (messageType)
            {
                case Constants.APPROVED_PLAYERS_MESSAGE_TYPE: return player.ApprovedPlayersMessageId;
                case Constants.TEAM_POLL_MESSAGE_TYPE: return player.PollMessageId;
                default: throw new ArgumentOutOfRangeException(nameof(messageType));
            }
        }

        public async Task DeleteMessageAsync(ChatId chatId, int messageId)
        {
            try
            {
                await _client.DeleteMessageAsync(chatId, messageId, cancellationToken: _cts.Token);
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
                return await SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
            }
        }

        private async Task ProcessRequests(List<Task<Message>> requests, Dictionary<int, Player> playersRequestsIds, Action<Player, Message> actionOnSuccess = null)
        {
            while (requests.Count > 0)
            {
                var response = await Task.WhenAny(requests);
                requests.Remove(response);
                var player = playersRequestsIds.First(r => r.Key == response.Id).Value;

                if (response.IsFaulted || response.IsCanceled)
                {
                    var errorMessage = response.IsFaulted ? response.Exception.Message : $"Тайм-аут {Constants.ASYNC_OPERATION_TIMEOUT} мс";
                    _logger.Error($"Error for user {player.Name}: {errorMessage}");
                    return;
                }

                actionOnSuccess?.Invoke(player, response.Result);
            }
        }

        public async Task<Message> EditMessageTextAsync(ChatId chatId, int messageId, string text)
        {
            return await _client.EditMessageTextAsync(chatId, messageId, text, cancellationToken: _cts.Token);
        }

        public async Task ClearReplyMarkupAsync(ChatId chatId, int messageId)
        {
            await _client.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: new[] { new InlineKeyboardButton[0] }, cancellationToken: _cts.Token);
        }
    }
}

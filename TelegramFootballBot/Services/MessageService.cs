using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramFootballBot.Data;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Services
{
    public class MessageService
    {
        public IPlayerRepository PlayerRepository { get; }

        private readonly ILogger _logger;   
        private readonly TelegramBotClient _client;  
        private readonly TeamsService _teamsService;
        private readonly MessageCallbackService _messageCallbackService;
        private string _approvedPlayersMessage = null;
        private string _likesMessage = null;
        private bool _isRunning = false;
        
        public MessageService(IPlayerRepository playerRepository, TeamsService teamsServiceSet, ILogger logger)
        {
            PlayerRepository = playerRepository;
            _teamsService = teamsServiceSet;
            _logger = logger;
            _client = new Bot().GetBotClient();            
            _messageCallbackService = new MessageCallbackService(_client, _teamsService, PlayerRepository, _logger);
        }

        public void Run()
        {
            if (_isRunning)
                throw new ApplicationException("MessageService is already running");

            _client.OnMessage += OnMessageRecievedAsync;
            _client.OnCallbackQuery += _messageCallbackService.OnCallbackQueryAsync;
            _client.StartReceiving();
            _isRunning = true;
        }

        public void Stop()
        {
            _client.OnMessage -= OnMessageRecievedAsync;
            _client.OnCallbackQuery -= _messageCallbackService.OnCallbackQueryAsync;
            _client.StopReceiving();
            _isRunning = false;
        }

        private async void OnMessageRecievedAsync(object sender, MessageEventArgs e)
        {
            var command = Bot.Commands.FirstOrDefault(c => c.StartsWith(e.Message));
            if (command == null)
                return;

            try
            {                
                await command.Execute(e.Message, this);
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Information($"Command {e.Message.Text} processed for user {playerName}");
            }
            catch (Exception ex)
            {
                var playerName = await GetPlayerNameAsync(e.Message.From.Id);
                _logger.Error(ex, $"Error on processing {e.Message.Text} command for user {playerName}");
                await SendTextMessageToBotOwnerAsync($"Ошибка у пользователя {playerName}: {ex.Message}");
                await _client.SendErrorMessageToUser(e.Message.Chat.Id, playerName);                
            }
        }

        public async Task SendMessageToAllUsersAsync(string text, IReplyMarkup replyMarkup = null)
        {
            await SendMessageAsync(await PlayerRepository.GetAllAsync(), text, replyMarkup);
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
            await EditMessageAsync(await PlayerRepository.GetRecievedMessageAsync(), _approvedPlayersMessage, Constants.APPROVED_PLAYERS_MESSAGE_TYPE);
        }

        public async Task UpdatePollMessagesAsync()
        {
            var likesMessage = _teamsService.LikesMessage();
            if (likesMessage == _likesMessage)
                return;

            _likesMessage = likesMessage;
            var playersVoted = (await PlayerRepository.GetAllAsync()).Where(p => p.PollMessageId != 0);
            await EditMessageAsync(playersVoted, _likesMessage, Constants.TEAM_POLL_MESSAGE_TYPE);
        }

        public async Task SendTeamPollMessageAsync()
        {            
            var pollMessage = _teamsService.GenerateMessage();
            if (string.IsNullOrEmpty(pollMessage))
                return;

            _likesMessage = _teamsService.LikesMessage();
            var players = await PlayerRepository.GetReadyToPlayAsync();
            await SendMessageAsync(players, pollMessage, MarkupHelper.GetTeamPollMarkup(_teamsService.GetActivePollId()));
        }

        public async Task SendTextMessageToBotOwnerAsync(string text, IReplyMarkup replyMarkup = null)
        {
            if (AppSettings.NotifyOwner)
                await _client.SendTextMessageToBotOwnerAsync(text, replyMarkup);
        }
        
        public async Task<Message> SendMessageAsync(ChatId chatId, string text, IReplyMarkup replyMarkup = null)
        {
            return await _client.SendTextMessageWithTokenAsync(chatId, text, replyMarkup);
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
                var request = _client.EditMessageTextWithTokenAsync(player.ChatId, MessageId(messageType, player), text);
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
                await _client.DeleteMessageWithTokenAsync(chatId, messageId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on deleting message");
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
                
        private async Task<string> GetPlayerNameAsync(int userId)
        {
            try
            {
                return (await PlayerRepository.GetAsync(userId)).Name;
            }
            catch (UserNotFoundException)
            {
                return string.Empty;
            }
        }
    }
}

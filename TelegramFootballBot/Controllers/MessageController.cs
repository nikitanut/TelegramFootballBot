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

namespace TelegramFootballBot.Controllers
{
    public class MessageController
    {
        public IPlayerRepository PlayerRepository { get; }

        private readonly ILogger _logger;   
        private readonly TelegramBotClient _client;  
        private readonly TeamsController _teamSet;
        private readonly MessageCallbackController _messageCallbackController;
        private string _approvedPlayersMessage = null;
        private bool _isRunning = false;
        
        public MessageController(IPlayerRepository playerRepository, TeamsController teamSet, ILogger logger)
        {
            PlayerRepository = playerRepository;
            _teamSet = teamSet;
            _logger = logger;
            _client = new Bot().GetBotClient();            
            _messageCallbackController = new MessageCallbackController(_client, _teamSet, PlayerRepository, _logger);
        }

        public void Run()
        {
            if (_isRunning)
                throw new ApplicationException("MessageController is already running");

            _client.OnMessage += OnMessageRecievedAsync;
            _client.OnCallbackQuery += _messageCallbackController.OnCallbackQueryAsync;
            _client.StartReceiving();
            _isRunning = true;
        }

        public void Stop()
        {
            _client.OnMessage -= OnMessageRecievedAsync;
            _client.OnCallbackQuery -= _messageCallbackController.OnCallbackQueryAsync;
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
            var gameDate = Scheduler.GetNearestGameDateMoscowTime(DateTime.UtcNow);
            var message = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";
            var markup = MarkupHelper.GetUserDeterminationMarkup(gameDate);
            await SendMessageToAllUsersAsync(message, markup);
        }

        public async Task UpdateTotalPlayersMessagesAsync()
        {
            var approvedPlayersMessage = await SheetController.GetInstance().GetApprovedPlayersMessageAsync();
            if (approvedPlayersMessage == _approvedPlayersMessage)
                return;

            _approvedPlayersMessage = approvedPlayersMessage;
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();

            foreach (var player in await PlayerRepository.GetReadyToPlayAsync())
            {
                var request = _client.EditMessageTextWithTokenAsync(player.ChatId, player.ApprovedPlayersMessageId, approvedPlayersMessage);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ProcessRequests(requests, playersRequestsIds);
        }

        public async Task SendTeamPollMessageAsync()
        {            
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();
            var message = string.Join(Environment.NewLine, _teamSet.GetRandom().Select(t => string.Join(Environment.NewLine, t)));
            //await SendMessageAsync(await PlayerRepository.GetReadyToPlayAsync(), message, MarkupHelper.GetTeamPollMarkup(_teamSet.GetActivePollId()));
            await SendTextMessageToBotOwnerAsync(message, MarkupHelper.GetTeamPollMarkup(_teamSet.GetActivePollId()));
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
        
        private async Task SendMessageAsync(IEnumerable<Player> players, string text, IReplyMarkup replyMarkup = null)
        {
            var requests = new List<Task<Message>>();
            var playersRequestsIds = new Dictionary<int, Player>();

            foreach (var player in players)
            {
                var request = SendMessageAsync(player.ChatId, text, replyMarkup);
                requests.Add(request);
                playersRequestsIds.Add(request.Id, player);
            }

            await ProcessRequests(requests, playersRequestsIds);
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
        
        private async Task ProcessRequests(List<Task<Message>> requests, Dictionary<int, Player> playersRequestsIds)
        {
            while (requests.Count > 0)
            {
                var response = await Task.WhenAny(requests);
                requests.Remove(response);

                if (response.IsFaulted || response.IsCanceled)
                {                    
                    var playerName = playersRequestsIds.First(r => r.Key == response.Id).Value.Name;
                    var errorMessage = response.IsFaulted ? response.Exception.Message : $"Тайм-аут {Constants.ASYNC_OPERATION_TIMEOUT} мс";
                    _logger.Error($"Error for user {playerName}: {errorMessage}");               
                }
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

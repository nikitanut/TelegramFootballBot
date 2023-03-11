using MediatR;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Exceptions;
using TelegramFootballBot.Core.Models;
using TelegramFootballBot.Core.Services;
using TelegramFootballBot.Queue.Messages;

namespace TelegramFootballBot.App.Handlers
{
    public class RefreshPlayersMessageHandler : IRequestHandler<RefreshPlayersMessages>
    {
        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISheetService _sheetService;
        private readonly ILogger _logger;

        public RefreshPlayersMessageHandler(IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService, ILogger logger)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
            _sheetService = sheetService;
            _logger = logger;
        }

        public async Task Handle(RefreshPlayersMessages request, CancellationToken cancellationToken)
        {
            try
            {
                var text = await _sheetService.BuildApprovedPlayersMessageAsync();
                var playersWithOutdatedMessage = await _playerRepository.GetPlayersWithOutdatedMessage(text);

                var messagesToRefresh = GetMessagesToRefresh(playersWithOutdatedMessage);
                var responses = await _messageService.EditMessagesAsync(text, messagesToRefresh);

                var errorResponses = responses.Where(r => r.Status == SendStatus.Error).ToList();
                await UpdateApprovedPlayersMessage(playersWithOutdatedMessage, errorResponses, text);

                if (errorResponses.Any())
                {
                    var message = string.Join(". ", errorResponses.Select(r => $"User error ({r.ChatId}) - {r.Message}"));
                    throw new SendMessageException(message);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.Error($"The operation was canceled for {nameof(RefreshPlayersMessageHandler)}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error on updating total players messages, {nameof(RefreshPlayersMessageHandler)}");
                await _messageService.SendMessageToBotOwnerAsync($"Ошибка при обновлении сообщений с отметившимися игроками: {ex.Message}");
            }
        }

        private static IEnumerable<Message> GetMessagesToRefresh(List<Player> playersWithOldMessage)
        {
            return playersWithOldMessage.Select(p => new Message
            {
                Text = p.ApprovedPlayersMessage,
                MessageId = p.ApprovedPlayersMessageId,
                Chat = new Chat { Id = p.ChatId }
            });
        }

        private async Task UpdateApprovedPlayersMessage(List<Player> players, List<SendMessageResponse> errorResponses, string message)
        {
            var playersDidNotGetMessage = players.Join(errorResponses, p => p.ChatId, r => r.ChatId, (p, r) => p);
            var playersGotMessage = players.Except(playersDidNotGetMessage).ToList();

            foreach (var player in playersGotMessage)
                player.ApprovedPlayersMessage = message;

            await _playerRepository.UpdateMultipleAsync(playersGotMessage);
        }
    }
}

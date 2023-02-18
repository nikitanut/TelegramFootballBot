using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Data;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class StatusCommand : Command
    {
        public override string Name => "/status";

        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;

        public StatusCommand(IMessageService messageService, IPlayerRepository playerRepository)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
        }

        public override async Task Execute(Message message)
        {
            if (!IsBotOwner(message))
                return;

            var players = await _playerRepository.GetAllAsync();
            var text = $"Now: {DateTime.Now.ToMoscowTime()}{Environment.NewLine}" +
                       $"Distribution: {AppSettings.DistributionTime}{Environment.NewLine}" +
                       $"GameDate: {AppSettings.GameDay}{Environment.NewLine}" +
                       $"Nearest Distribution: {DateHelper.GetNearestDistributionDateMoscowTime(DateTime.UtcNow)}{Environment.NewLine}" +
                       $"Nearest GameDate: {DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow)}{Environment.NewLine}" +
                       $"Players: {players.Count}{Environment.NewLine}" +
                       $"Got message: {players.Count(p => p.ApprovedPlayersMessageId != 0)}";

            await _messageService.SendMessageAsync(message.Chat.Id, text);
        }
    }
}

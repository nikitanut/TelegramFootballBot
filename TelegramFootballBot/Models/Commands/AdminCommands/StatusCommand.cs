using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class StatusCommand : Command
    {
        public override string Name => "/status";

        public override async Task Execute(Message message, MessageService messageService)
        {
            if (!IsBotOwner(message))
                return;

            var players = await messageService.PlayerRepository.GetAllAsync();
            var text = $"Now: {DateTime.Now.ToMoscowTime()}{Environment.NewLine}" +
                       $"Distribution: {AppSettings.DistributionTime}{Environment.NewLine}" +
                       $"GameDate: {AppSettings.GameDay}{Environment.NewLine}" +
                       $"Nearest Distribution: {DateHelper.GetNearestDistributionDateMoscowTime(DateTime.UtcNow)}{Environment.NewLine}" +
                       $"Nearest GameDate: {DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow)}{Environment.NewLine}" +
                       $"Players: {players.Count}{Environment.NewLine}" +
                       $"Got message: {players.Count(p => p.ApprovedPlayersMessageId != 0)}";

            await messageService.SendMessageAsync(message.Chat.Id, text);
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class StatusCommand : Command
    {
        public override string Name => "/status";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (!IsBotOwner(message))
                return;

            var players = await messageController.PlayerRepository.GetAllAsync();
            var text = $"Now: {DateTime.Now.ToMoscowTime()}{Environment.NewLine}" +
                       $"Distribution: {AppSettings.DistributionTime}{Environment.NewLine}" +
                       $"GameDate: {AppSettings.GameDay}{Environment.NewLine}" +
                       $"Nearest Distribution: {DateHelper.GetNearestDistributionDateMoscowTime(DateTime.UtcNow)}{Environment.NewLine}" +
                       $"Nearest GameDate: {DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow)}{Environment.NewLine}" +
                       $"Players: {players.Count}{Environment.NewLine}" +
                       $"Got message: {players.Count(p => p.ApprovedPlayersMessageId != 0)}";

            await messageController.SendMessageAsync(message.Chat.Id, text);
        }
    }
}

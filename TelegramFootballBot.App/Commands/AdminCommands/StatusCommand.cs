using Telegram.Bot.Types;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Helpers;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Commands.AdminCommands;

public class StatusCommand(IMessageService messageService, IPlayerRepository playerRepository) : Command
{
    public override string Name => "/status";

    public override async Task ExecuteAsync(Message message)
    {
        if (!IsBotOwner(message))
            return;

        var players = await playerRepository.GetAllAsync();
        var playerNamesAndIds = string.Join(Environment.NewLine, players.Select(p => $"    {p.Name} ({p.Id})"));

        var text = $"Now: {DateTime.Now.ToMoscowTime()}{Environment.NewLine}" +
                   $"Distribution: {AppSettings.DistributionTime}{Environment.NewLine}" +
                   $"GameDate: {AppSettings.GameDay}{Environment.NewLine}" +
                   $"Nearest Distribution: {DateHelper.GetNearestDistributionDateMoscowTime(DateTime.UtcNow)}{Environment.NewLine}" +
                   $"Nearest GameDate: {DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow)}{Environment.NewLine}" +
                   $"Players ({players.Count}):{Environment.NewLine}{playerNamesAndIds}{Environment.NewLine}" +
                   $"Got message: {players.Count(p => p.ApprovedPlayersMessageId != 0)}";

        await messageService.SendMessageAsync(message.Chat.Id, text);
    }
}

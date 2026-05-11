using Telegram.Bot.Types;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Commands.AdminCommands;

public class BanCommand(IMessageService messageService, IPlayerRepository playerRepository) : Command
{
    public override string Name => "/ban";

    public override async Task ExecuteAsync(Message message)
    {
        if (!IsBotOwner(message))
            return;

        var playerId = GetPlayerId(message);
        var player = playerId > 0 ? await playerRepository.GetAsync(playerId) : null;

        if (player is null)
        {
            await messageService.SendMessageToBotOwnerAsync("Player not found");
            return;
        }

        player.IsBanned = true;
        await playerRepository.UpdateAsync(player);
        await messageService.SendMessageToBotOwnerAsync($"{player.Name} was banned");
    }

    private long GetPlayerId(Message message)
    {
        var playerIdString = message.Text!.Length > Name.Length ? message.Text[Name.Length..].Trim() : string.Empty;
        long.TryParse(playerIdString, out var playerId);
        return playerId;
    }
}

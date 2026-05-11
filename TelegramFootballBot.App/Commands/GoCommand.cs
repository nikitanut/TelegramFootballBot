using Telegram.Bot.Types;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Exceptions;
using TelegramFootballBot.App.Helpers;
using TelegramFootballBot.App.Models;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Commands;

public class GoCommand(IMessageService messageService, IPlayerRepository playerRepository) : Command
{
    public override string Name => "/go";

    public override async Task ExecuteAsync(Message message)
    {
        Player player;
        try
        {
            player = await playerRepository.GetAsync(message.From!.Id);
        }
        catch (UserNotFoundException)
        {
            await messageService.SendMessageAsync(message.Chat.Id, $"Вы не были зарегистрированы{Environment.NewLine}Введите /reg Фамилия Имя");
            return;
        }

        if (player.IsBanned)
            return;

        var gameDate = DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow);
        var text = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";

        await messageService.DeleteMessageAsync(message.Chat.Id, message.MessageId);
        await messageService.SendMessageAsync(player.ChatId, text, MarkupHelper.GetIfReadyToPlayQuestion(gameDate));
    }
}

using Telegram.Bot.Types;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Commands;

public class InfoCommand(IMessageService messageService) : Command
{
    public override string Name => "/info";

    public override async Task ExecuteAsync(Message message)
    {
        await messageService.SendMessageAsync(message.Chat.Id, Text(message));
    }

    private static string Text(Message message)
    {
        if (IsBotOwner(message))
        {
            return $"/status - get statistics{Environment.NewLine}" +
                   $"/ban {{playerId}} - ban player";
        }

        return $"/reg - зарегистрироваться{Environment.NewLine}" +
               $"/go - отметиться";
    }
}

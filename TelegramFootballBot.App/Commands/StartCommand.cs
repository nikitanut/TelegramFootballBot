using Telegram.Bot.Types;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Commands;

public class StartCommand(IMessageService messageService) : Command
{
    public override string Name => "/start";

    public override async Task ExecuteAsync(Message message)
    {
        await messageService.SendMessageAsync(message.Chat.Id, "Для регистрации введите /reg Фамилия Имя");
    }
}

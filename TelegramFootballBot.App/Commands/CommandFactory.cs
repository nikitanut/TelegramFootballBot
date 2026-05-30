using Telegram.Bot.Types;
using TelegramFootballBot.App.Commands.AdminCommands;
using TelegramFootballBot.App.Data;
using TelegramFootballBot.App.Services;

namespace TelegramFootballBot.App.Commands;

public class CommandFactory
{
    private readonly List<Command> _commands;

    public CommandFactory(IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService)
    {
        _commands = new List<Command>
        {
            new RegisterCommand(messageService, playerRepository, sheetService),
            new GoCommand(messageService, playerRepository),
            new StatusCommand(messageService, playerRepository),
            new InfoCommand(messageService),
            new StartCommand(messageService),
            new BanCommand(messageService, playerRepository)
        };
    }

    public Command? Create(Message name)
    {
        return _commands.FirstOrDefault(c => c.StartsWith(name));
    }
}

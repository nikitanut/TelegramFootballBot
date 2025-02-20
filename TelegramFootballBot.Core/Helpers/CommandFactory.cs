﻿using Telegram.Bot.Types;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Models.Commands;
using TelegramFootballBot.Core.Models.Commands.AdminCommands;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Helpers
{
    public class CommandFactory
    {
        private readonly List<Command> _commands;

        public CommandFactory(IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService)
        {
            _commands = new List<Command>
            {
                new RegisterCommand(messageService, playerRepository, sheetService),
                new UnregisterCommand(messageService, playerRepository),
                new GoCommand(messageService, playerRepository),
                new SwitchNotifierCommand(messageService),
                new SayCommand(messageService, playerRepository),
                new StatusCommand(messageService, playerRepository),
                new InfoCommand(messageService),
                new StartCommand(messageService),
            };
        }

        public Command? Create(Message name)
        {
            return _commands.FirstOrDefault(c => c.StartsWith(name));
        }
    }
}

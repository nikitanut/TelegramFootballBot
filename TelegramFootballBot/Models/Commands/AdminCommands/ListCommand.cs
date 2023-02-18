using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Data;
using TelegramFootballBot.Services;

namespace TelegramFootballBot.Models.Commands.AdminCommands
{
    public class ListCommand : Command
    {
        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;

        public override string Name => "/list";

        public ListCommand(IMessageService messageService, IPlayerRepository playerRepository)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
        }

        public override async Task Execute(Message message)
        {
            if (!IsBotOwner(message))
                return;

            var players = (await _playerRepository.GetAllAsync()).OrderBy(p => p.Name);

            var text = string.Join(Environment.NewLine, players.Select(p => $"{p.Name} - {p.Rating}"));
            await _messageService.SendMessageAsync(message.Chat.Id, text);
        }
    }
}

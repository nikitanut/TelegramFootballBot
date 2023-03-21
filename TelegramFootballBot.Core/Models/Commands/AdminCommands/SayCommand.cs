using Telegram.Bot.Types;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Models.Commands.AdminCommands
{
    public class SayCommand : Command
    {
        public override string Name => "/say";

        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;

        public SayCommand(IMessageService messageService, IPlayerRepository playerRepository)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
        }

        public override async Task ExecuteAsync(Message message)
        {
            if (!IsBotOwner(message))
                return;

            var text = message.Text!.Length > Name.Length
                ? message.Text[Name.Length..].Trim()
                : string.Empty;

            if (text != string.Empty)
            {
                var players = await _playerRepository.GetAllAsync();
                var chatIds = players.Select(p => (ChatId)p.ChatId);
                await _messageService.SendMessagesAsync(text, chatIds);
            }
        }
    }
}

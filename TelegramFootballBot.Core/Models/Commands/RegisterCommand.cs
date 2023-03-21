using Telegram.Bot.Types;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Exceptions;
using TelegramFootballBot.Core.Services;

namespace TelegramFootballBot.Core.Models.Commands
{
    public class RegisterCommand : Command
    {
        public override string Name => "/reg";

        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISheetService _sheetService;

        public RegisterCommand(IMessageService messageService, IPlayerRepository playerRepository, ISheetService sheetService)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
            _sheetService = sheetService;
        }

        public override async Task ExecuteAsync(Message message)
        {
            var playerName = GetPlayerNameFrom(message);
            if (playerName == string.Empty)
            {
                await _messageService.SendMessageAsync(message.Chat.Id, $"Вы не указали фамилию и имя{Environment.NewLine}Введите /reg Фамилия Имя");
                return;
            }

            var messageForUser = await RegisterPlayer(message);

            await Task.WhenAll(new[]
            {
                _messageService.SendMessageAsync(message.Chat.Id, messageForUser),
                _sheetService.UpsertPlayerAsync(playerName),
                _messageService.SendMessageToBotOwnerAsync($"{playerName} зарегистрировался")
            });
        }

        private string GetPlayerNameFrom(Message message)
        {
            return message.Text!.Length > Name.Length ? message.Text[Name.Length..].Trim() : string.Empty;
        }

        private async Task<string> RegisterPlayer(Message message)
        {
            var playerName = GetPlayerNameFrom(message);

            try
            {
                var existingPlayer = await _playerRepository.GetAsync(message.From!.Id);
                var messageForUser = existingPlayer.Name == playerName ? "Вы уже зарегистрированы" : "Вы уже были зарегистрированы. Имя обновлено.";
                existingPlayer.Name = playerName;
                await _playerRepository.UpdateAsync(existingPlayer);
                return messageForUser;
            }
            catch (UserNotFoundException)
            {
                var newPlayer = new Player(message.From!.Id, playerName, message.Chat.Id);
                await _playerRepository.AddAsync(newPlayer);
                return "Регистрация прошла успешно";
            }
        }
    }
}
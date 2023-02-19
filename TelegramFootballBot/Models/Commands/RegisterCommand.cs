using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Core.Services;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Exceptions;

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
            return message.Text.Length > Name.Length ? message.Text[Name.Length..].Trim() : string.Empty;
        }

        private async Task<string> RegisterPlayer(Message message)
        {
            try
            {
                var existPlayer = await _playerRepository.GetAsync(message.From.Id);
                var messageForUser = existPlayer.Name == GetPlayerNameFrom(message) ? "Вы уже зарегистрированы" : "Вы уже были зарегистрированы. Имя обновлено.";
                existPlayer.Name = GetPlayerNameFrom(message);
                await _playerRepository.UpdateAsync(existPlayer);
                return messageForUser;
            }
            catch (UserNotFoundException)
            {
                await _playerRepository.AddAsync(new Player(message.From.Id, GetPlayerNameFrom(message), message.Chat.Id));
                return "Регистрация прошла успешно";
            }
        }
    }
}
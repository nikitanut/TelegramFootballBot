using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;
using TelegramFootballBot.Data;

namespace TelegramFootballBot.Models.Commands
{
    public class RegisterCommand : Command
    {
        public override string Name => "/reg";

        public override async Task Execute(Message message, MessageService messageService)
        {
            if (PlayerName(message) == string.Empty)
            {
                await messageService.SendMessageAsync(message.Chat.Id, $"Вы не указали фамилию и имя{Environment.NewLine}Введите /reg Фамилия Имя");
                return;
            }

            var messageForUser = await RegisterPlayer(message, messageService.PlayerRepository);
            await messageService.SendMessageAsync(message.Chat.Id, messageForUser);
            await SheetService.GetInstance().UpsertPlayerAsync(PlayerName(message));
            await messageService.SendTextMessageToBotOwnerAsync($"{PlayerName(message)} зарегистрировался");
        }

        private string PlayerName(Message message)
        {
            return message.Text.Length > Name.Length ? message.Text.Substring(Name.Length).Trim() : string.Empty;
        }

        private async Task<string> RegisterPlayer(Message message, IPlayerRepository playerRepository)
        {
            try
            {
                var existPlayer = await playerRepository.GetAsync(message.From.Id);
                var messageForUser = existPlayer.Name == PlayerName(message) ? "Вы уже зарегистрированы" : "Вы уже были зарегистрированы. Имя обновлено.";
                existPlayer.Name = PlayerName(message);
                await playerRepository.UpdateAsync(existPlayer);
                return messageForUser;
            }
            catch (UserNotFoundException)
            {
                await playerRepository.AddAsync(new Player(message.From.Id, PlayerName(message), message.Chat.Id));
                return "Регистрация прошла успешно";
            }
        }
    }
}
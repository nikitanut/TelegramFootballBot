using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Data;

namespace TelegramFootballBot.Models.Commands
{
    public class RegisterCommand : Command
    {
        public override string Name => "/reg";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (PlayerName(message) == string.Empty)
            {
                await messageController.SendMessageAsync(message.Chat.Id, $"Вы не указали фамилию и имя{Environment.NewLine}Введите /reg Фамилия Имя");
                return;
            }

            var messageForUser = await RegisterPlayer(message, messageController.PlayerRepository);
            await messageController.SendMessageAsync(message.Chat.Id, messageForUser);
            await SheetController.GetInstance().UpsertPlayerAsync(PlayerName(message));
            await messageController.SendTextMessageToBotOwnerAsync($"{PlayerName(message)} зарегистрировался");
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
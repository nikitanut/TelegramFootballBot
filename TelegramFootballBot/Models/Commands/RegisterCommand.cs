using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

namespace TelegramFootballBot.Models.Commands
{
    public class RegisterCommand : Command
    {
        public override string Name => "/reg";

        public override async Task Execute(Message message, MessageController messageController)
        {
            var userName = message.Text.Length > Name.Length
                ? message.Text.Substring(Name.Length).Trim()
                : string.Empty;

            if (userName == string.Empty)
            {
                await messageController.SendMessageAsync(message.Chat.Id, $"Вы не указали фамилию и имя{Environment.NewLine}Введите /reg Фамилия Имя");
                return;
            }
            
            var messageForUser = "Регистрация прошла успешно";

            try
            {
                var existPlayer = await messageController.PlayerRepository.GetAsync(message.From.Id);                
                messageForUser = existPlayer.Name == userName ? "Вы уже зарегистрированы" : "Вы уже были зарегистрированы. Имя обновлено.";
                existPlayer.Name = userName;

                await messageController.PlayerRepository.UpdateAsync(existPlayer);
            }
            catch (UserNotFoundException)
            {
                await messageController.PlayerRepository.AddAsync(new Player(message.From.Id, userName, message.Chat.Id));
            }
            
            await messageController.SendMessageAsync(message.Chat.Id, messageForUser);
            await SheetController.GetInstance().UpsertPlayerAsync(userName);
            await messageController.SendTextMessageToBotOwnerAsync($"{userName} зарегистрировался");
        }
    }
}

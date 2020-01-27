using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class RegisterCommand : Command
    {
        public override string Name => "/reg";

        public override async Task Execute(Message message, TelegramBotClient client)
        {
            var userName = message.Text.Length > Name.Length
                ? message.Text.Substring(Name.Length).Trim()
                : string.Empty;

            if (userName == string.Empty)
            {
                await client.SendTextMessageWithTokenAsync(message.Chat.Id, $"Вы не указали фамилию и имя{Environment.NewLine}Введите /reg Фамилия Имя");
                return;
            }
            
            var messageForUser = "Регистрация прошла успешно";
            try
            {
                var existPlayer = await Bot.GetPlayerAsync(message.From.Id);                
                messageForUser = existPlayer.Name == userName ? "Вы уже зарегистрированы" : "Вы уже были зарегистрированы. Имя обновлено.";
                existPlayer.Name = userName;
                await Bot.UpdatePlayerAsync(existPlayer);
            }
            catch (UserNotFoundException)
            {
                await Bot.AddNewPlayerAsync(new Player(message.From.Id, userName, message.Chat.Id));
            }
            
            await client.SendTextMessageWithTokenAsync(message.Chat.Id, messageForUser);
            await SheetController.GetInstance().UpsertPlayerAsync(userName);
            await client.SendTextMessageToBotOwnerAsync($"{userName} зарегистрировался");
        }
    }
}

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
        public override string Name => "/register";

        public override async Task Execute(Message message, TelegramBotClient client)
        {
            var userName = message.Text.Length > Name.Length
                ? message.Text.Substring(Name.Length).Trim()
                : string.Empty;

            if (userName == string.Empty)
            {
                await client.SendTextMessageWithTokenAsync(message.Chat.Id, $"Вы не указали фамилию и имя{Environment.NewLine}Введите /register *Фамилия* *Имя*");
                return;
            }

            Player existPlayer = null;
            var messageForUser = "Регистрация прошла успешно";
            
            try
            {
                existPlayer = await Bot.GetPlayerAsync(message.From.Id);
                existPlayer.Name = userName;

                if (!existPlayer.IsActive)
                    existPlayer.IsActive = true;
                else
                    messageForUser = existPlayer.Name == userName ? "Вы уже зарегистрированы" : "Имя обновлено";
            }
            catch (UserNotFoundException) { }
            
            await client.SendTextMessageWithTokenAsync(message.Chat.Id, messageForUser);

            if (existPlayer == null)
                await Bot.AddNewPlayerAsync(new Player(message.From.Id, userName, message.Chat.Id));
            else
                await Bot.UpdatePlayerAsync(existPlayer);

            await SheetController.GetInstance().UpsertPlayerAsync(userName);
            await client.SendTextMessageToBotOwnerAsync($"{userName} зарегистрировался");
        }
    }
}

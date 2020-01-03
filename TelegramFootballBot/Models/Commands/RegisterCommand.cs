using System;
using System.Linq;
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

            var existPlayer = Bot.Players.FirstOrDefault(p => p.Id == message.From.Id);
            string messageForUser;

            if (existPlayer == null || !existPlayer.IsActive)
            {
                existPlayer.Name = userName;
                existPlayer.IsActive = true;
                messageForUser = "Регистрация прошла успешно";
            }
            else messageForUser = "Вы уже зарегистрированы";

            await client.SendTextMessageWithTokenAsync(message.Chat.Id, messageForUser);

            if (existPlayer == null)
                Bot.AddNewPlayer(new Player(message.From.Id, userName, message.Chat.Id));

            await SheetController.GetInstance().UpsertPlayerAsync(userName);
            await client.SendTextMessageToBotOwnerAsync($"{userName} зарегистрировался");
        }
    }
}

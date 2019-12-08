using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;

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
                await client.SendTextMessageAsync(message.Chat.Id, $"Вы не указали фамилию и имя{Environment.NewLine}Введите /register *Фамилия* *Имя*");
                return;
            }

            var existPlayer = Bot.Players.FirstOrDefault(p => p.Id == message.From.Id);
            if (existPlayer != null)
            {
                existPlayer.Name = userName;
                existPlayer.IsActive = true;
                await client.SendTextMessageAsync(message.Chat.Id, $"Игрок {userName} зарегистрирован");
                await Bot.UpdatePlayers();
                await SheetController.GetInstance().UpsertPlayer(userName);
            }
            else
            {
                await client.SendTextMessageAsync(message.Chat.Id, $"Игрок {userName} зарегистрирован");
                var player = new Player(message.From.Id, userName, message.Chat.Id);                
                await Bot.AddNewPlayer(player);
                await SheetController.GetInstance().UpsertPlayer(userName);
            }
        }
    }
}

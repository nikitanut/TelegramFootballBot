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
            var text = ProcessMessage(message.From.Id, message.Text, message.Chat.Id);
            await client.SendTextMessageAsync(message.Chat.Id, text);
        }

        private string ProcessMessage(int userId, string messageText, long chatId)
        {
            var userName = messageText.Length > Name.Length 
                ? messageText.Substring(Name.Length).Trim()
                : string.Empty;

            if (userName == string.Empty)
                return $"Вы не указали фамилию и имя{Environment.NewLine}Введите /register *Фамилия* *Имя*";

            var existPlayer = Bot.Players.FirstOrDefault(p => p.Id == userId);
            if (existPlayer != null)
            {
                existPlayer.Name = userName;
                existPlayer.IsActive = true;
                Task.Run(async () => { await FileController.UpdatePlayers(Bot.Players); }).Wait();
                // TODO: Update excel
                return $"Вы уже были зарегистрированы{Environment.NewLine}Имя изменено на {userName}";
            }

            var player = new Player(userId, userName, chatId);
            // TODO: Update excel
            var updateResult = Task.Run(async () => await Bot.AddNewPlayer(player)).Result;

            return updateResult ? $"Игрок {userName} зарегистрирован" : $"Не удалось зарегистрировать игрока {userName}";
        }
    }
}

using System;
using System.Linq;
using System.Threading;
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
                var emptyUserNameCancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                await client.SendTextMessageAsync(message.Chat.Id, $"Вы не указали фамилию и имя{Environment.NewLine}Введите /register *Фамилия* *Имя*", cancellationToken: emptyUserNameCancellationToken);
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

            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await client.SendTextMessageAsync(message.Chat.Id, messageForUser, cancellationToken: cancellationToken);

            if (existPlayer != null)
                await Bot.UpdatePlayersAsync();
            else
            {
                var player = new Player(message.From.Id, userName, message.Chat.Id);
                await Bot.AddNewPlayerAsync(player);
            }

            await SheetController.GetInstance().UpsertPlayerAsync(userName);
        }
    }
}

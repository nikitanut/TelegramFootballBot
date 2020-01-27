using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class GoCommand : Command
    {
        public override string Name => "/go";

        public override async Task Execute(Message message, TelegramBotClient client)
        {
            Player player;
            try
            {
                player = await Bot.GetPlayerAsync(message.From.Id);
            }
            catch (UserNotFoundException)
            {
                await client.SendTextMessageWithTokenAsync(message.Chat.Id, $"Вы не были зарегистрированы{Environment.NewLine}Введите /reg *Фамилия* *Имя*");
                return;
            }

            var gameDate = Scheduler.GetGameDateMoscowTime(DateTime.UtcNow);
            var text = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";
            var markup = MarkupHelper.GetUserDeterminationMarkup(gameDate);

            await client.SendTextMessageWithTokenAsync(player.ChatId, text, markup);
        }
    }
}

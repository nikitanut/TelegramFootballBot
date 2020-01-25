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
                await client.SendTextMessageWithTokenAsync(message.Chat.Id, $"Вы не были зарегистрированы{Environment.NewLine}Введите /register *Фамилия* *Имя*");
                return;
            }

            var gameDate = Scheduler.GetGameDateMoscowTime(DateTime.UtcNow);
            var text = $"Идёшь на футбол {gameDate.ToString("dd.MM")}?";
            var markup = MarkupHelper.GetKeyBoardMarkup(MessageController.GetGameStartCallbackPrefix(gameDate), Constants.YES_ANSWER, Constants.NO_ANSWER);

            await client.SendTextMessageWithTokenAsync(player.ChatId, text, markup);
        }
    }
}

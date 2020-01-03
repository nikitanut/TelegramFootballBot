using System;
using System.Linq;
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
            var player = Bot.Players.FirstOrDefault(p => p.Id == message.From.Id);
            if (player == null)
            {
                await client.SendTextMessageWithTokenAsync(message.Chat.Id, $"Вы не были зарегистрированы{Environment.NewLine}Введите /register *Фамилия* *Имя*");
                return;
            }

            var text = $"Идёшь на футбол {Scheduler.GetGameDate(DateTime.Now).ToString("dd.MM")}?";
            var markup = MarkupHelper.GetKeyBoardMarkup(Constants.PLAYERS_SET_CALLBACK_PREFIX, Constants.YES_ANSWER, Constants.NO_ANSWER);
            await client.SendTextMessageWithTokenAsync(player.ChatId, text, markup);
        }
    }
}

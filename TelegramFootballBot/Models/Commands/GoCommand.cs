using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class GoCommand : Command
    {
        public override string Name => "/go";

        public override async Task Execute(Message message, MessageController messageController)
        {
            Player player;
            try
            {
                player = await messageController.PlayerRepository.GetAsync(message.From.Id);
            }
            catch (UserNotFoundException)
            {
                await messageController.SendMessageAsync(message.Chat.Id, $"Вы не были зарегистрированы{Environment.NewLine}Введите /reg Фамилия Имя");
                return;
            }

            var gameDate = DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow);
            var text = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";
            var markup = MarkupHelper.GetUserDeterminationMarkup(gameDate);

            await messageController.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            await messageController.SendMessageAsync(player.ChatId, text, markup);
        }
    }
}

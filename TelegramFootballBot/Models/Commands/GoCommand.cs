using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class GoCommand : Command
    {
        public override string Name => "/go";

        public override async Task Execute(Message message, MessageService messageService)
        {
            Player player;
            try
            {
                player = await messageService.PlayerRepository.GetAsync(message.From.Id);
            }
            catch (UserNotFoundException)
            {
                await messageService.SendMessageAsync(message.Chat.Id, $"Вы не были зарегистрированы{Environment.NewLine}Введите /reg Фамилия Имя");
                return;
            }

            var gameDate = DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow);
            var text = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";

            await messageService.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            await messageService.SendMessageAsync(player.ChatId, text, MarkupHelper.GetUserDeterminationMarkup(gameDate));
        }
    }
}

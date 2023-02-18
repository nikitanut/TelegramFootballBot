using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Services;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Data;

namespace TelegramFootballBot.Models.Commands
{
    public class GoCommand : Command
    {
        public override string Name => "/go";

        private readonly IMessageService _messageService;
        private readonly IPlayerRepository _playerRepository;

        public GoCommand(IMessageService messageService, IPlayerRepository playerRepository)
        {
            _messageService = messageService;
            _playerRepository = playerRepository;
        }

        public override async Task Execute(Message message)
        {
            Player player;
            try
            {
                player = await _playerRepository.GetAsync(message.From.Id);
            }
            catch (UserNotFoundException)
            {
                await _messageService.SendMessageAsync(message.Chat.Id, $"Вы не были зарегистрированы{Environment.NewLine}Введите /reg Фамилия Имя");
                return;
            }

            var gameDate = DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow);
            var text = $"Идёшь на футбол {gameDate.ToRussianDayMonthString()}?";

            await _messageService.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            await _messageService.SendMessageAsync(player.ChatId, text, MarkupHelper.GetUserDeterminationMarkup(gameDate));
        }
    }
}

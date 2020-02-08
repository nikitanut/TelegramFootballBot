﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Helpers;

namespace TelegramFootballBot.Models.Commands
{
    public class InfoCommand : Command
    {
        public override string Name => "/info";

        public override async Task Execute(Message message, MessageController messageController)
        {
            if (!IsBotOwner(message))
                return;

            var players = await messageController.PlayerRepository.GetAllAsync();
            var text = $"Now: {DateTime.Now.ToMoscowTime()}{Environment.NewLine}" +
                       $"Distribution: {AppSettings.DistributionTime}{Environment.NewLine}" +
                       $"GameDate: {AppSettings.GameDay}{Environment.NewLine}" +
                       $"Players: {players.Count()}{Environment.NewLine}" +
                       $"Got message: {players.Count(p => p.ApprovedPlayersMessageId != 0)}";
            
            await messageController.SendMessageAsync(message.Chat.Id, text);
        }
    }
}
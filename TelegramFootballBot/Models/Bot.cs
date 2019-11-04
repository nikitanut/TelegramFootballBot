﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramFootballBot.Models.Commands;
using TelegramFootballBot.Models.Processors;

namespace TelegramFootballBot.Models
{
    public class Bot
    {
        private TelegramBotClient botClient;
        public static List<Command> Commands { get; set; }
        public static List<Player> Players { get; set; }

        public TelegramBotClient GetBotClient()
        {
            if (botClient != null)
                return botClient;

            InitializeCommands();

            Players = Task.Run(async () => await FileProcessor.GetPlayers()).Result;
            botClient = new TelegramBotClient(AppSettings.BotToken);

            return botClient;
        }

        public static async Task<bool> AddNewPlayer(Player player)
        {
            if (player == null) return false;

            Players.Add(player);
            var isSerialized = await FileProcessor.UpdatePlayers(Players);
            return isSerialized;
        }

        private static void InitializeCommands()
        {
            Commands = new List<Command>();
            Commands.Add(new StartCommand());
            Commands.Add(new HelloCommand());
            Commands.Add(new RegisterCommand());
        }
    }
}

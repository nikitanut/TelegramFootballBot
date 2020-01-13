﻿namespace TelegramFootballBot.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }      
        public long ChatId { get; set; }
        public int ApprovedPlayersMessageId { get; set; }
        public bool IsGoingToPlay { get; set; }

        private Player() { }

        public Player(int id, string name, long chatid)
        {
            Id = id;
            Name = name;
            ChatId = chatid;
        }
    }
}

namespace TelegramFootballBot.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }      
        public long ChatId { get; set; }
        public bool IsActive { get; set; }
        public int TotalPlayersMessageId { get; set; }
       
        // For serialization
        private Player() { }

        public Player(int id, string name, long chatid)
        {
            Id = id;
            Name = name;
            ChatId = chatid;
            IsActive = true;
            TotalPlayersMessageId = 0;
        }
    }
}

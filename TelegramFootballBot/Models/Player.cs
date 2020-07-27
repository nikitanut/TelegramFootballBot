namespace TelegramFootballBot.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }      
        public int Rating { get; set; }
        public long ChatId { get; set; }
        public int ApprovedPlayersMessageId { get; set; }
        public int PollMessageId { get; set; }
        public bool IsGoingToPlay { get; set; }
        
        private Player() { }

        public Player(string name)
        {
            Name = name;
        }

        public Player(int id, string name, long chatid)
        {
            Id = id;
            Name = name;
            ChatId = chatid;
        }
    }
}

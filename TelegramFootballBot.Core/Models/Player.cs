namespace TelegramFootballBot.Core.Models
{
    public class Player
    {
        public long Id { get; set; }

        public string Name { get; set; } = default!;

        public long ChatId { get; set; }

        public int ApprovedPlayersMessageId { get; set; }

        public string ApprovedPlayersMessage { get; set; } = default!;

        public bool IsGoingToPlay { get; set; }

        private Player() { }

        public Player(string name)
        {
            Name = name;
        }

        public Player(long id, string name, long chatid)
        {
            Id = id;
            Name = name;
            ChatId = chatid;
        }
    }
}

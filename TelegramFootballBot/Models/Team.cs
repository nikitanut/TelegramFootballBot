using System;
using System.Collections.Generic;
using System.Linq;

namespace TelegramFootballBot.Models
{
    public class Team
    {
        public string Name { get; set; }

        public double AverageRating
        {
            get
            {
                return (double)Players.Select(p => p.Rating).Sum() / Players.Count;
            }
        }

        public List<Player> Players { get; set; }

        public Team(IEnumerable<Player> players)
        {
            Players = players.ToList();
        }

        public override string ToString()
        {
            return $"{Name}{Environment.NewLine}{string.Join(Environment.NewLine, Players.Select(p => p.Name))}";
        }
    }
}

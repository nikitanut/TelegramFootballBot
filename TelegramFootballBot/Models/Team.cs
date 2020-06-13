using System;
using System.Collections.Generic;
using System.Linq;
using TelegramFootballBot.Helpers;

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

        public Team()
        {
            Players = new List<Player>();
        }

        public Team(IEnumerable<Player> players)
        {
            Players = players.ToList();
        }

        public override string ToString()
        {
            return $"{Name}{Environment.NewLine}{MarkupHelper.GetDashedString()}{Environment.NewLine}" 
                + string.Join(Environment.NewLine, Players.Select(p => p.Name));
        }
    }
}

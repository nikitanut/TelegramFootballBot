using System.Collections.Generic;
using TelegramFootballBot.Core.Models;

namespace TelegramFootballBot.Tests
{
    public static class TestPlayerSet
    {
        private static readonly List<Player> _players;

        static TestPlayerSet()
        {
            var id = 1;
            _players = new List<Player>
            {
                new Player($"{id++}") { Rating = 84 },
                new Player($"{id++}") { Rating = 90 },
                new Player($"{id++}") { Rating = 88 },
                new Player($"{id++}") { Rating = 86 },
                new Player($"{id++}") { Rating = 82 },
                new Player($"{id++}") { Rating = 75 },
                new Player($"{id++}") { Rating = 60 },
                new Player($"{id++}") { Rating = 64 },
                new Player($"{id++}") { Rating = 73 },
                new Player($"{id++}") { Rating = 75 }
            };
        }

        public static IReadOnlyCollection<Player> Get()
        {
            return _players;
        }
    }
}

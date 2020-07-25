using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TelegramFootballBot.Models
{
    public static class TeamSet
    {
        public static event EventHandler OnDislike;

        private static List<List<Team>> _teamSets = new List<List<Team>>();
        private static List<Team> _activeTeamSet = new List<Team>();
        private static List<int> _dislikedTeamsIndices = new List<int>();
        private static int _likesForActive;
        private static int _dislikesForActive;

        public static IReadOnlyCollection<Team> GetActive()
        {
            return _activeTeamSet;
        }

        public static void SetActive(IEnumerable<IEnumerable<Team>> teamSets)
        {
            _teamSets = teamSets.Select(v => v.ToList()).ToList();
        }

        public static List<Team> GetRandom()
        {
            if (_teamSets.Count == _dislikedTeamsIndices.Count)
                return _teamSets.FirstOrDefault();

            int randomIndex;
            do randomIndex = new Random().Next(_teamSets.Count);
            while (_dislikedTeamsIndices.Contains(randomIndex));

            return _teamSets.ElementAt(randomIndex);
        }

        public static void LikeActive()
        {
            Interlocked.Increment(ref _likesForActive);
        }

        public static void DislikeActive()
        {
            if (Interlocked.Increment(ref _dislikesForActive) == 5) // TODO: implement dislike amount check
                OnDislike.Invoke(typeof(TeamSet), new EventArgs());
        }
    }
}

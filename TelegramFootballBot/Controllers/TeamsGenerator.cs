using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Controllers
{
    public static class TeamsGenerator
    {
        private const int MAX_PLAYERS = 15;

        public static List<List<Team>> Generate(IEnumerable<Player> players)
        {
            foreach (var playerWithoutRating in players.Where(p => p.Rating == 0))
                playerWithoutRating.Rating = Constants.DEFAULT_PLAYER_RATING;

            var playersToDistribute = players
                .OrderByDescending(p => p.Rating)
                .Take(MAX_PLAYERS).ToList();

            var variantsCombinations = new KeyValuePair<List<Team>, double?>[10];
            for (var i = 0; i < variantsCombinations.Length; i++)
                variantsCombinations[i] = new KeyValuePair<List<Team>, double?>(null, double.MaxValue);

            foreach (var line in File.ReadLines($"Variants for {playersToDistribute.Count()}"))
            {
                var teams = line.Split(',')
                    .Select(c => c.Split(' ').Select(i => Convert.ToInt32(i)))
                    .Select(teamIndices => new Team(teamIndices.Select(i => playersToDistribute.ElementAt(i))));

                var index = 0;
                var maxDeltaVariant = new KeyValuePair<List<Team>, double?>(null, double.MinValue);

                for (int i = 0; i < variantsCombinations.Length; i++)
                {
                    if (maxDeltaVariant.Value < variantsCombinations[i].Value)
                    {
                        maxDeltaVariant = variantsCombinations[i];
                        index = i;
                    }
                }

                var maxDelta = maxDeltaVariant.Value ?? double.MaxValue;
                var currentDelta = CountDelta(teams);

                if (currentDelta < maxDelta //&& !variantsCombinations.Any(v => v.Value == currentDelta)
                    )
                    variantsCombinations[index] = new KeyValuePair<List<Team>, double?>(teams.ToList(), currentDelta);
            }

            foreach (var combination in variantsCombinations)
            {
                var teamIndex = 0;
                var remainingPlayers = new Stack<Player>(players.Where(p => !combination.Key.Any(t => t.Players.Contains(p))));

                while (remainingPlayers.Any())
                {
                    if (teamIndex == combination.Key.Count())
                        teamIndex = 0;

                    combination.Key.Skip(teamIndex).First().Players.Add(remainingPlayers.Pop());
                    teamIndex++;
                }
            }

            foreach (var team in variantsCombinations.SelectMany(v => v.Key))
                team.Players = team.Players.OrderBy(p => p.Name).ToList();

            return variantsCombinations.Select(v => v.Key).ToList();
        }
                
        private static double CountDelta(IEnumerable<Team> teamsToCheck)
        {
            return teamsToCheck.Max(t => t.AverageRating) - teamsToCheck.Min(t => t.AverageRating);
        }            
    }
}

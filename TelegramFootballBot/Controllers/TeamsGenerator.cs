using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Controllers
{
    public static class TeamsGenerator
    {
        private const int MAX_PLAYERS = 15;

        public static List<Team> Generate(IEnumerable<Player> players)
        {
            var playersToDistribute = players.Where(p => p.Rating != 0).OrderByDescending(p => p.Rating).Take(MAX_PLAYERS).ToList();
            var teamsDelta = double.MaxValue;
            IEnumerable<Team> variants = new List<Team>();

            foreach (var line in File.ReadLines($"Variants for {playersToDistribute.Count()}"))
            {
                var teams = line.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(i => Convert.ToInt32(i)))
                    .Select(teamIndices => new Team(teamIndices.Select(i => playersToDistribute.ElementAt(i))));

                if (CountDelta(teams) < teamsDelta)
                {
                    variants = teams;
                    teamsDelta = CountDelta(teams);
                }
            }

            variants = variants.OrderByDescending(t => t.AverageRating).ToList();
            
            var teamIndex = 0;
            var remainingPlayers = new Stack<Player>(players.Where(p => !variants.Any(t => t.Players.Contains(p))));
                        
            while (remainingPlayers.Any())
            {
                if (teamIndex == variants.Count())
                    teamIndex = 0;
                
                variants.Skip(teamIndex).First().Players.Add(remainingPlayers.Pop());
                teamIndex++;                
            }

            return variants.ToList();
        }
                
        private static double CountDelta(IEnumerable<Team> teamsToCheck)
        {
            return teamsToCheck.Max(t => t.AverageRating) - teamsToCheck.Min(t => t.AverageRating);
        }            
    }
}

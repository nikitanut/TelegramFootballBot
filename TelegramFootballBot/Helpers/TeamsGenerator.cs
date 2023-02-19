using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TelegramFootballBot.Core.Models;

namespace TelegramFootballBot.Core.Helpers
{
    public static class TeamsGenerator
    {
        private const int MIN_PLAYERS = 10;
        private const int MAX_PLAYERS = 14;

        public static List<List<Team>> Generate(IEnumerable<Player> players)
        {
            SetUnknownPlayersDefaultRating(players);
            var teamSets = GenerateTeamSets(players);
            FillRemainingPlayers(players, teamSets);
            SetTeamNames(teamSets);
            return ToOrderedTeamSets(teamSets);
        }

        private static void SetUnknownPlayersDefaultRating(IEnumerable<Player> players)
        {
            var playersWithoutRating = players.Where(p => p.Rating == 0);
            foreach (var player in playersWithoutRating)
                player.Rating = Constants.DEFAULT_PLAYER_RATING;
        }
        
        private static KeyValuePair<List<Team>, double?>[] GenerateTeamSets(IEnumerable<Player> players)
        {
            var generatingSets = DefaultSet();
            var playersToDistribute = players.OrderByDescending(p => p.Rating).Take(MAX_PLAYERS).ToList();
            if (playersToDistribute.Count < MIN_PLAYERS)
                return Array.Empty<KeyValuePair<List<Team>, double?>>();
            
            foreach (var line in File.ReadLines($"Variants for {playersToDistribute.Count}"))
                UpdateTeamSetRatings(TeamSetFromFile(playersToDistribute, line), ref generatingSets);

            return generatingSets.Where(v => v.Key != null).ToArray();
        }

        private static IEnumerable<Team> TeamSetFromFile(List<Player> playersToDistribute, string fileLine)
        {
            return fileLine.Split(',')
                .Select(set => set.Split(' ').Select(indexString => Convert.ToInt32(indexString)))
                .Select(teamIndices => new Team(teamIndices.Select(i => playersToDistribute.ElementAt(i))));
        }

        private static void FillRemainingPlayers(IEnumerable<Player> players, KeyValuePair<List<Team>, double?>[] teamSets)
        {
            foreach (var set in teamSets)
            {
                var orderedByRatingTeams = set.Key.OrderBy(t => t.AverageRating);
                var teamIndex = 0;
                var remainingPlayers = new Stack<Player>(players.Where(p => !orderedByRatingTeams.Any(t => t.Players.Contains(p))));

                while (remainingPlayers.Any())
                {
                    if (teamIndex == orderedByRatingTeams.Count())
                        teamIndex = 0;

                    orderedByRatingTeams.Skip(teamIndex).First().Players.Add(remainingPlayers.Pop());
                    teamIndex++;
                }
            }
        }

        private static void SetTeamNames(KeyValuePair<List<Team>, double?>[] teamSets)
        {
            var allTeams = teamSets.SelectMany(v => v.Key).ToArray();
            var teamsNames = NamesGenerator.Generate(allTeams.Length);
            for (int i = 0; i < allTeams.Length; i++)
                allTeams[i].Name = teamsNames[i];
        }
        
        private static List<List<Team>> ToOrderedTeamSets(KeyValuePair<List<Team>, double?>[] teamSets)
        {
            foreach (var set in teamSets.Select(t => t.Key))
                foreach (var team in set)
                    team.Players.Sort((a, b) => a.Name.CompareTo(b.Name));

            return teamSets.Select(t => t.Key).ToList();
        }

        private static KeyValuePair<List<Team>, double?>[] DefaultSet()
        {
            var generatingSets = new KeyValuePair<List<Team>, double?>[Constants.TEAM_VARIANTS_TO_GENERATE];
            for (var i = 0; i < generatingSets.Length; i++)
                generatingSets[i] = new KeyValuePair<List<Team>, double?>(null, double.MaxValue);
            return generatingSets;
        }

        private static void UpdateTeamSetRatings(IEnumerable<Team> currentSet, ref KeyValuePair<List<Team>, double?>[] generatedSets)
        {
            var index = 0;
            var maxDeltaVariant = new KeyValuePair<List<Team>, double?>(null, double.MinValue);

            for (int i = 0; i < generatedSets.Length; i++)
            {
                if (maxDeltaVariant.Value < generatedSets[i].Value)
                {
                    maxDeltaVariant = generatedSets[i];
                    index = i;
                }
            }

            var maxDelta = maxDeltaVariant.Value ?? double.MaxValue;
            var currentDelta = Delta(currentSet);
            var isDuplicate = generatedSets.Any(v => v.Value == currentDelta);

            if (currentDelta < maxDelta && !isDuplicate)
                generatedSets[index] = new KeyValuePair<List<Team>, double?>(currentSet.ToList(), currentDelta);
        }

        private static double Delta(IEnumerable<Team> teamsToCheck)
        {
            return teamsToCheck.Max(t => t.AverageRating) - teamsToCheck.Min(t => t.AverageRating);
        }            
    }
}
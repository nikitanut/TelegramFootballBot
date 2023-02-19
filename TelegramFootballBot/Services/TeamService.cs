using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Core.Data;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core.Models;
using TelegramFootballBot.Core.Models.CallbackQueries;

namespace TelegramFootballBot.Core.Services
{
    public class TeamService : ITeamService
    {
        public int ActiveLikes => _likesForCurrentTeam;
        public int ActiveDislikes => _dislikesForActive;
        public bool IsActiveDisliked { get; private set;}

        private readonly IPlayerRepository _playerRepository;
        private List<List<Team>> _teamSets = new();
        private List<Team> _currentTeamSet = new();
        private readonly List<List<Team>> _dislikedTeams = new();
        private Guid _activePollId;
        private int _likesForCurrentTeam;
        private int _dislikesForActive;

        public TeamService(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        public async Task GenerateNewTeams(IEnumerable<string> playersNames)
        {
            IsActiveDisliked = false;
            GeneratePollId();
            _likesForCurrentTeam = 0;
            _dislikesForActive = 0;
            
            var playersReadyToPlay = (await _playerRepository.GetAllAsync())
                .Where(p => playersNames.Contains(p.Name)).ToList();

            playersReadyToPlay.AddRange(playersNames
                .Where(n => !playersReadyToPlay.Any(p => p.Name == n))
                .Select(n => new Player(n)));
            
            _teamSets = TeamsGenerator.Generate(playersReadyToPlay);            
            SetActiveTeamSet(GetRandomTeamSet());
        }   

        public Guid GeneratePollId()
        {
            _activePollId = Guid.NewGuid();
            return GetActivePollId();
        }

        public IReadOnlyCollection<Team> CurrentTeamSet()
        {
            return _currentTeamSet;
        }

        public string GenerateMessageWithTeamSet()
        {
            return string.Join(Environment.NewLine + Environment.NewLine, CurrentTeamSet().Select(t => string.Join(Environment.NewLine, t)));
        }

        public string GetMessageWithLikes()
        {
            return $"{Constants.LIKE_EMOJI} - {_likesForCurrentTeam}   {Constants.DISLIKE_EMOJI} - {_dislikesForActive}";
        }

        public void ClearGeneratedTeams()
        {
            _likesForCurrentTeam = 0;
            _dislikesForActive = 0;
            _dislikedTeams.Clear();
            _teamSets.Clear();
            SetActiveTeamSet(GetRandomTeamSet());
        }
               
        private void SetActiveTeamSet(IEnumerable<Team> teamSet)
        {
            _currentTeamSet = teamSet.ToList();
        }

        public List<Team> GetRandomTeamSet()
        {
            if (_teamSets.Count == _dislikedTeams.Count || !_teamSets.Any())
                return new List<Team>();

            List<Team> randomTeamSet;
            do randomTeamSet = _teamSets.ElementAt(new Random().Next(_teamSets.Count));
            while (_dislikedTeams.Contains(randomTeamSet));
                        
            return randomTeamSet;
        }

        public void LikeCurrentTeam()
        {
            Interlocked.Increment(ref _likesForCurrentTeam);
        }

        public void DislikeCurrentTeam()
        {
            var activeDislikes = Interlocked.Increment(ref _dislikesForActive);

            if (activeDislikes == Constants.TEAM_DISLIKES_LIMIT)
            {
                IsActiveDisliked = true;
                _dislikedTeams.Add(_currentTeamSet);
            }
        }

        public void ProcessPollChoice(TeamPollCallback teamCallback)
        {
            if (teamCallback.PollId != GetActivePollId()) return;
            if (teamCallback.UserAnswer == Constants.YES_ANSWER) LikeCurrentTeam();
            if (teamCallback.UserAnswer == Constants.NO_ANSWER) DislikeCurrentTeam();
        }

        public Guid GetActivePollId()
        {
            return _activePollId != default ? _activePollId : GeneratePollId();
        }
    }
}

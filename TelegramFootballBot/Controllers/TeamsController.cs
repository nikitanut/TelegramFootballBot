using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Controllers;
using TelegramFootballBot.Data;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models.CallbackQueries;

namespace TelegramFootballBot.Models
{
    public class TeamsController
    {
        public int ActiveLikes => _likesForActive;
        public int ActiveDislikes => _dislikesForActive;
        public bool IsActiveDisliked { get; private set;}

        private readonly IPlayerRepository _playerRepository;
        private List<List<Team>> _teamSets = new List<List<Team>>();
        private List<Team> _activeTeamSet = new List<Team>();
        private List<List<Team>> _dislikedTeams = new List<List<Team>>();
        private Guid _activePollId;
        private int _likesForActive;
        private int _dislikesForActive;

        public TeamsController(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
        }

        public async Task GenerateNewTeams(IEnumerable<string> playersNames)
        {
            IsActiveDisliked = false;
            GeneratePollId();
            _likesForActive = 0;
            _dislikesForActive = 0;
            
            var playersReadyToPlay = (await _playerRepository.GetAllAsync())
                .Where(p => playersNames.Contains(p.Name)).ToList();

            playersReadyToPlay.AddRange(playersNames
                .Where(n => !playersReadyToPlay.Any(p => p.Name == n))
                .Select(n => new Player(n)));
            
            _teamSets = TeamsGenerator.Generate(playersReadyToPlay);            
            SetActive(GetRandom());
        }   

        public Guid GeneratePollId()
        {
            _activePollId = Guid.NewGuid();
            return GetActivePollId();
        }

        public IReadOnlyCollection<Team> GetActive()
        {
            return _activeTeamSet;
        }

        public string GenerateMessage()
        {
            return string.Join(Environment.NewLine + Environment.NewLine, GetActive().Select(t => string.Join(Environment.NewLine, t)));
        }

        public string LikesMessage()
        {
            return $"{Constants.LIKE_EMOJI} - {_likesForActive}   {Constants.DISLIKE_EMOJI} - {_dislikesForActive}";
        }

        public void ClearGeneratedTeams()
        {
            _likesForActive = 0;
            _dislikesForActive = 0;
            _dislikedTeams.Clear();
            _teamSets.Clear();
            SetActive(GetRandom());
        }
               
        private void SetActive(IEnumerable<Team> teamSet)
        {
            _activeTeamSet = teamSet.ToList();
        }

        public List<Team> GetRandom()
        {
            if (_teamSets.Count == _dislikedTeams.Count || !_teamSets.Any())
                return new List<Team>();

            List<Team> randomTeam;
            do randomTeam = _teamSets.ElementAt(new Random().Next(_teamSets.Count));
            while (_dislikedTeams.Contains(randomTeam));
                        
            return randomTeam;
        }

        public void LikeActive()
        {
            Interlocked.Increment(ref _likesForActive);
        }

        public void DislikeActive()
        {
            var activeDislikes = Interlocked.Increment(ref _dislikesForActive);

            if (activeDislikes == Constants.TEAM_DISLIKES_LIMIT)
            {
                IsActiveDisliked = true;
                _dislikedTeams.Add(_activeTeamSet);
            }
        }

        public void ProcessPollChoice(TeamPollCallback teamCallback)
        {
            if (teamCallback.PollId != GetActivePollId()) return;
            if (teamCallback.UserAnswer == Constants.YES_ANSWER) LikeActive();
            if (teamCallback.UserAnswer == Constants.NO_ANSWER) DislikeActive();
        }

        public Guid GetActivePollId()
        {
            return _activePollId != default ? _activePollId : GeneratePollId();
        }
    }
}

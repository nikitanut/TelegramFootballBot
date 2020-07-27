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
        public event EventHandler OnDislike;

        public int ActiveLikes => _likesForActive;
        public int ActiveDislikes => _dislikesForActive;

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
            OnDislike += (o, e) =>
            {
                _dislikedTeams.Add(_activeTeamSet);
                GenerateNewTeams().Wait();
            };
        }

        public async Task GenerateNewTeams()
        {
            GeneratePollId();
            _likesForActive = 0;
            _dislikesForActive = 0;
            
            var playersNames = await SheetController.GetInstance().GetPlayersReadyToPlay();
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
            return string.Join(Environment.NewLine, GetActive().Select(t => string.Join(Environment.NewLine, t)));
        }

        public string LikesMessage()
        {
            return $"За - {_likesForActive}. Против - {_dislikesForActive}.";
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
            if (_teamSets.Count == _dislikedTeams.Count)
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
            var halfOfPlayers = GetActive().Sum(t => t.Players.Count) / 2;
            var activeDislikes = Interlocked.Increment(ref _dislikesForActive);
            if (activeDislikes == halfOfPlayers)
                OnDislike.Invoke(null, null);
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

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
        private List<int> _dislikedTeamsIndices = new List<int>();
        private Guid _activePollId;
        private int _likesForActive;
        private int _dislikesForActive;

        public TeamsController(IPlayerRepository playerRepository)
        {
            _playerRepository = playerRepository;
            OnDislike += (o, e) => GenerateNewTeams().Wait();
        }

        public async Task GenerateNewTeams()
        {
            var playersNames = await SheetController.GetInstance().GetPlayersReadyToPlay();
            var playersReadyToPlay = (await _playerRepository.GetAllAsync())
                .Where(p => playersNames.Contains(p.Name)).ToList();

            playersReadyToPlay.AddRange(playersNames
                .Where(n => !playersReadyToPlay.Any(p => p.Name == n))
                .Select(n => new Player(n)));
            
            _teamSets = TeamsGenerator.Generate(playersReadyToPlay);
            SetActive(GetRandom());
            GeneratePollId();
            _likesForActive = 0;
            _dislikesForActive = 0;
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

        private void SetActive(IEnumerable<Team> teamSet)
        {
            _activeTeamSet = teamSet.ToList();
        }

        public List<Team> GetRandom()
        {
            if (_teamSets.Count == _dislikedTeamsIndices.Count)
                return _teamSets.FirstOrDefault();

            int randomIndex;
            do randomIndex = new Random().Next(_teamSets.Count);
            while (_dislikedTeamsIndices.Contains(randomIndex));

            _dislikedTeamsIndices.Add(randomIndex);
            return _teamSets.ElementAt(randomIndex);
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

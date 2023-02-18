using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramFootballBot.Core.Models;
using TelegramFootballBot.Core.Models.CallbackQueries;

namespace TelegramFootballBot.Core.Services
{
    public interface ITeamService
    {
        bool IsActiveDisliked { get; }

        Task GenerateNewTeams(IEnumerable<string> playersNames);

        Guid GeneratePollId();

        IReadOnlyCollection<Team> CurrentTeamSet();

        string GenerateMessageWithTeamSet();

        string GetMessageWithLikes();

        void ClearGeneratedTeams();

        List<Team> GetRandomTeamSet();

        void LikeCurrentTeam();

        void DislikeCurrentTeam();

        void ProcessPollChoice(TeamPollCallback teamCallback);

        Guid GetActivePollId();
    }
}

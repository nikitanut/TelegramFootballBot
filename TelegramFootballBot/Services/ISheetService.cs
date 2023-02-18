using System.Collections.Generic;
using System.Threading.Tasks;

namespace TelegramFootballBot.Core.Services
{
    public interface ISheetService
    {
        Task UpdateApproveCellAsync(string playerName, string cellValue);

        Task UpsertPlayerAsync(string playerName);

        Task ClearApproveCellsAsync();

        Task<string> GetApprovedPlayersMessageAsync();

        Task<List<string>> GetPlayersReadyToPlayAsync();
    }
}

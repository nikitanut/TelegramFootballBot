using System.Collections.Generic;
using System.Threading.Tasks;

namespace TelegramFootballBot.Core.Services
{
    public interface ISheetService
    {
        Task SetApproveCellAsync(string playerName, string cellValue);

        Task UpsertPlayerAsync(string playerName);

        Task ClearGameCellsAsync();

        Task<string> BuildApprovedPlayersMessageAsync();

        Task<List<string>> GetPlayersReadyToPlayAsync();
    }
}

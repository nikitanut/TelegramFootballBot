using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Helpers;
using ValueRange = Google.Apis.Sheets.v4.Data.ValueRange;

namespace TelegramFootballBot.Controllers
{
    public class SheetController
    {
        private static SheetController _sheetController;    
        
        private readonly SheetsService _sheetsService;        

        private SheetController()
        {
            _sheetsService = GetSheetsService();
        }

        public static SheetController GetInstance()
        {
            if (_sheetController == null)
                _sheetController = new SheetController();
            return _sheetController;
        }

        private SheetsService GetSheetsService()
        {
            GoogleCredential credential;

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "TelegramFootballBot"
            });
        }

        public async Task UpdateApproveCellAsync(string playerName, string cellValue)
        {            
            var sheet = await GetSheetAsync();
            var userRow = SheetHelper.GetUserRowNumber(sheet.Values, playerName);

            if (userRow == -1)
                userRow = await CreateNewPlayerRowAsync(sheet.Values, sheet.Range, playerName);

            var range = SheetHelper.GetUserRange(userRow);
            var request = _sheetsService.Spreadsheets.Values.Update(SheetHelper.GetValueRange(range, cellValue), AppSettings.GoogleDocSheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await request.ExecuteAsync(new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token);
        }

        public async Task UpsertPlayerAsync(string playerName)
        {
            var sheet = await GetSheetAsync();
            if (SheetHelper.GetUserRowNumber(sheet.Values, playerName) == -1)
                await CreateNewPlayerRowAsync(sheet.Values, sheet.Range, playerName);
        }
        
        public async Task ClearApproveCellsAsync()
        {
            var sheet = await GetSheetAsync();            
            var players = SheetHelper.GetOrderedPlayers(sheet.Values);

            var dateOfNextGame = DateHelper.GetNearestGameDateMoscowTime(DateTime.Now).AddDays(7);
            var newValues = new List<IList<object>>
            {
                new List<object>() { dateOfNextGame.ToRussianDayMonthString() }
            };

            foreach (var player in players)
                newValues.Add(new List<object>() { string.Empty });
            
            await UpdateSheetAsync(newValues, SheetHelper.GetApproveColumnRange(sheet.Values, players.Count));
        }

        public async Task<string> GetApprovedPlayersMessageAsync()
        {
            var sheet = await GetSheetAsync();            
            var players = SheetHelper.GetOrderedPlayers(sheet.Values);
            return SheetHelper.GetApprovedPlayersString(players);            
        }

        public async Task<List<string>> GetPlayersReadyToPlay()
        {
            var sheet = await GetSheetAsync();
            return SheetHelper.GetPlayersReadyToPlay(sheet.Values);
        }

        private async Task<ValueRange> GetSheetAsync()
        {
            var request = _sheetsService.Spreadsheets.Values.Get(AppSettings.GoogleDocSheetId, SheetHelper.GetAllUsersRange());
            return await request.ExecuteAsync(new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token);
        }

        private async Task<int> CreateNewPlayerRowAsync(IList<IList<object>> values, string range, string playerName)
        {
            if (SheetHelper.GetUserRowNumber(values, playerName) != -1)
                throw new ArgumentException($"Player {playerName} already exists.");

            var players = SheetHelper.GetOrderedPlayers(values, playerName);
            var newValues = SheetHelper.GetNewValues(values, players);

            // Add empty rows to clear unnecesary data
            var endEmptyRows = 30;
            while (--endEmptyRows != 0)
                newValues.Add(new List<object> { string.Empty, string.Empty});
                        
            await UpdateSheetAsync(newValues, range);
            await UpdateLastRowStyle(values.IndexOf(SheetHelper.GetTotalsRow(values)), newValues.IndexOf(SheetHelper.GetTotalsRow(values)));
            return SheetHelper.GetUserRowNumber(newValues, playerName);
        }
             
        private async Task UpdateLastRowStyle(int oldTotalsRowIndex, int newTotalsRowIndex)
        {
            if (oldTotalsRowIndex == newTotalsRowIndex)
                return;

            var cutPasteStyleRequest = SheetHelper.GetMoveCellsStyleRequest(oldTotalsRowIndex, newTotalsRowIndex);
            var updateRequest = _sheetsService.Spreadsheets.BatchUpdate(cutPasteStyleRequest, AppSettings.GoogleDocSheetId);
            await updateRequest.ExecuteAsync(new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token);
        }

        private async Task<BatchUpdateValuesResponse> UpdateSheetAsync(IList<IList<object>> values, string range)
        {
            var updateDataRequest = SheetHelper.GetBatchUpdateRequest(values, range);
            var request = _sheetsService.Spreadsheets.Values.BatchUpdate(updateDataRequest, AppSettings.GoogleDocSheetId);
            return await request.ExecuteAsync(new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token);
        }
    }
}


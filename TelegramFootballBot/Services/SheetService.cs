using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Core.Helpers;
using ValueRange = Google.Apis.Sheets.v4.Data.ValueRange;

namespace TelegramFootballBot.Core.Services
{
    public class SheetService : ISheetService
    {
        private readonly SheetsService _sheetsService;
        private readonly string _googleDocSheetId;

        public SheetService(Stream credentialsFile, string googleDocSheetId)
        {
            _sheetsService = CreateSheetsService(credentialsFile);
            _googleDocSheetId = googleDocSheetId;
        }

        private static SheetsService CreateSheetsService(Stream credentialsFile)
        {
            var credential = GoogleCredential.FromStream(credentialsFile).CreateScoped(SheetsService.Scope.Spreadsheets);
            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "TelegramFootballBot"
            });
        }

        public async Task SetApproveCellAsync(string playerName, string cellValue)
        {            
            var sheet = await GetSheetAsync();
            var playerRow = SheetHelper.GetPlayerRowNumber(sheet.Values, playerName);

            if (playerRow == -1)
                playerRow = await CreateNewPlayerRowAsync(sheet.Values, sheet.Range, playerName);

            var playerRange = SheetHelper.GetPlayerRange(playerRow);
            var request = _sheetsService.Spreadsheets.Values.Update(SheetHelper.ToValueRange(playerRange, cellValue), _googleDocSheetId, playerRange);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            
            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            await request.ExecuteAsync(cts.Token);
        }

        public async Task UpsertPlayerAsync(string playerName)
        {
            var sheet = await GetSheetAsync();
            var playerExists = SheetHelper.GetPlayerRowNumber(sheet.Values, playerName) != -1;
            if (!playerExists)
                await CreateNewPlayerRowAsync(sheet.Values, sheet.Range, playerName);
        }
        
        public async Task ClearGameCellsAsync()
        {
            var sheet = await GetSheetAsync();            
            var players = SheetHelper.GetOrderedPlayers(sheet.Values);

            var dateOfNextGame = DateHelper.GetNearestGameDateMoscowTime(DateTime.Now).AddDays(7);
            var newValues = new List<IList<object>>
            {
                new List<object>() { dateOfNextGame.ToRussianDayMonthString() }
            };

            for (var i = 0; i < players.Count; i++)
                newValues.Add(new List<object>() { string.Empty });
            
            await UpdateSheetAsync(newValues, SheetHelper.GetApproveColumnRange(sheet.Values, players.Count));
        }

        public async Task<string> BuildApprovedPlayersMessageAsync()
        {
            var sheet = await GetSheetAsync();            
            var players = SheetHelper.GetOrderedPlayers(sheet.Values);
            return SheetHelper.BuildApprovedPlayersMessage(players);            
        }

        public async Task<List<string>> GetPlayersReadyToPlayAsync()
        {
            var sheet = await GetSheetAsync();
            return SheetHelper.GetPlayersReadyToPlay(sheet.Values);
        }

        private async Task<ValueRange> GetSheetAsync()
        {
            var request = _sheetsService.Spreadsheets.Values.Get(_googleDocSheetId, SheetHelper.GetAllPlayersRange());
            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            return await request.ExecuteAsync(cts.Token);
        }

        private async Task<int> CreateNewPlayerRowAsync(IList<IList<object>> values, string range, string playerName)
        {
            var playerExists = SheetHelper.GetPlayerRowNumber(values, playerName) != -1;
            if (playerExists)
                throw new ArgumentException($"Player {playerName} already exists.");

            var players = SheetHelper.GetOrderedPlayers(values, playerName);
            var newValues = SheetHelper.ApplyPlayers(values, players);

            // Add empty rows to clear unnecessary data
            var endEmptyRows = 30;
            while (--endEmptyRows != 0)
                newValues.Add(new List<object> { string.Empty, string.Empty});
                        
            await UpdateSheetAsync(newValues, range);
            await UpdateLastRowStyle(values.IndexOf(SheetHelper.GetTotalsRow(values)), newValues.IndexOf(SheetHelper.GetTotalsRow(values)));
            return SheetHelper.GetPlayerRowNumber(newValues, playerName);
        }
             
        private async Task UpdateLastRowStyle(int oldTotalsRowIndex, int newTotalsRowIndex)
        {
            if (oldTotalsRowIndex == newTotalsRowIndex)
                return;

            var cutPasteStyleRequest = SheetHelper.GetMoveCellsStyleRequest(oldTotalsRowIndex, newTotalsRowIndex);
            var updateRequest = _sheetsService.Spreadsheets.BatchUpdate(cutPasteStyleRequest, _googleDocSheetId);

            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            await updateRequest.ExecuteAsync(cts.Token);
        }

        private async Task<BatchUpdateValuesResponse> UpdateSheetAsync(IList<IList<object>> values, string range)
        {
            var updateDataRequest = SheetHelper.GetBatchUpdateRequest(values, range);
            var request = _sheetsService.Spreadsheets.Values.BatchUpdate(updateDataRequest, _googleDocSheetId);

            using var cts = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT);
            return await request.ExecuteAsync(cts.Token);
        }
    }
}


﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramFootballBot.Helpers;
using TelegramFootballBot.Models;
using ValueRange = Google.Apis.Sheets.v4.Data.ValueRange;

namespace TelegramFootballBot.Controllers
{
    public class SheetController
    {
        private static SheetController _sheetController;

        private enum Column { A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z }

        private const Column NAME_COLUMN = Column.A;
        private const Column APPROVE_COLUMN = Column.B;
        private const string SHEET_NAME = "Участие в играх";
        private const string TOTAL_LABEL = "Всего";

        private readonly string[] _scopes = { SheetsService.Scope.Spreadsheets };        
        private readonly SheetsService _sheetsService;
        private readonly Dictionary<int, string> _monthNames = new Dictionary<int, string>()
        {
            { 1, "января" }, { 2, "февраля" }, { 3, "марта" }, { 4, "апреля" }, { 5, "мая" }, { 6, "июня" },
            { 7, "июля" }, { 8, "августа" }, { 9, "сентября" }, { 10, "октября" }, { 11, "ноября" }, { 12, "декабря" }
        };

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
            var binPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(_scopes);
            }

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "TelegramFootballBot"
            });
        }

        public async Task<string> UpdateApproveCellAsync(string playerName, string cellValue)
        {            
            var sheet = await GetSheetAsync();
            var userRow = GetUserRowNumber(playerName, sheet);

            if (userRow == -1)
                userRow = await CreateNewPlayerRowAsync(sheet.Values, sheet.Range, playerName);

            var range = $"{SHEET_NAME}!{APPROVE_COLUMN}{userRow}";
            var request = _sheetsService.Spreadsheets.Values.Update(GetValueRange(range, cellValue), AppSettings.GoogleDocSheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            var response = await request.ExecuteAsync(cancellationToken);

            return JsonConvert.SerializeObject(response);
        }

        public async Task UpsertPlayerAsync(string playerName)
        {
            var sheet = await GetSheetAsync();
            var userRaw = GetUserRowNumber(playerName, sheet);

            if (userRaw == -1)
                await CreateNewPlayerRowAsync(sheet.Values, sheet.Range, playerName);
        }

        public async Task<int> GetTotalApprovedPlayersAsync()
        {
            var sheet = await GetSheetAsync();
            var startRowsToIgnore = GetStartRows(sheet.Values).Count();
            var players = GetOrderedPlayers(sheet.Values, startRowsToIgnore);
            return GetTotalApprovedPlayers(players);
        }

        private int GetTotalApprovedPlayers(IList<IList<object>> players)
        {
            return players.Sum(p =>
            {
                if (p.Count <= (int)APPROVE_COLUMN) return 0;
                double.TryParse(p[(int)APPROVE_COLUMN]?.ToString(), out double approveValue);
                return (int)approveValue;
            });
        }

        public async Task ClearApproveCellsAsync()
        {
            var sheet = await GetSheetAsync();
            var startRowsToIgnore = GetStartRows(sheet.Values).Count();
            var players = GetOrderedPlayers(sheet.Values, startRowsToIgnore);

            var dateOfNextGame = Scheduler.GetGameDate(DateTime.Now);
            var newValues = new List<IList<object>>();
            newValues.Add(new List<object>() { $"{dateOfNextGame.Day} {_monthNames[dateOfNextGame.Month]}" });

            foreach (var player in players)
                newValues.Add(new List<object>() { string.Empty });

            var dateOfGameCell = $"{APPROVE_COLUMN}{startRowsToIgnore}";
            var lastPlayerCell = $"{APPROVE_COLUMN}{startRowsToIgnore + players.Count}";

            await UpdateSheetAsync(newValues, $"{dateOfGameCell}:{lastPlayerCell}");
        }

        public async Task<string> GetApprovedPlayersMessageAsync()
        {
            var sheet = await GetSheetAsync();
            var startRowsToIgnore = GetStartRows(sheet.Values).Count();
            var players = GetOrderedPlayers(sheet.Values, startRowsToIgnore);
            return GetApprovedPlayersString(players);            
        }

        private string GetApprovedPlayersString(IList<IList<object>> players)
        {
            var totalApprovedPlayers = $"Всего отметилось: {GetTotalApprovedPlayers(players)}";
            var playersNames = players.Where(p =>
            {
                if (p.Count <= (int)APPROVE_COLUMN) return false;
                int.TryParse(p[(int)APPROVE_COLUMN]?.ToString(), out int approveValue);
                return approveValue > 0;
            })
            .Select(p =>
            {
                int.TryParse(p[(int)APPROVE_COLUMN].ToString(), out int countByPlayer);
                var playerName = p[(int)NAME_COLUMN].ToString();
                return countByPlayer == 1 ? playerName : playerName + " x" + countByPlayer;
            });

            return totalApprovedPlayers
                + Environment.NewLine
                + Environment.NewLine
                + string.Join(Environment.NewLine, playersNames);
        }

        private int GetUserRowNumber(string playerName, ValueRange sheet)
        {
            if (playerName == null)
                return -1;

            var userRow = sheet.Values.FirstOrDefault(v => v.Any(o => playerName.Equals(o.ToString(), StringComparison.InvariantCultureIgnoreCase)));
            var userRowIndex = sheet.Values.IndexOf(userRow);
            return userRowIndex != -1 ? userRowIndex + 1 : -1;
        }

        private async Task<ValueRange> GetSheetAsync()
        {
            var range = $"{SHEET_NAME}!{NAME_COLUMN}:{APPROVE_COLUMN}";
            var request = _sheetsService.Spreadsheets.Values.Get(AppSettings.GoogleDocSheetId, range);
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            return await request.ExecuteAsync(cancellationToken);
        }

        private async Task<int> CreateNewPlayerRowAsync(IList<IList<object>> values, string range, string playerName)
        {
            var playerExists = values.Any(v => CellEqualsValue(v, (int)NAME_COLUMN, playerName));
            if (playerExists)
                throw new ArgumentException($"Player {playerName} already exists.");

            var startRowsToIgnoreCount = GetStartRows(values).Count();
            var players = GetOrderedPlayers(values, startRowsToIgnoreCount, playerName);
            var newPlayerRowNumber = startRowsToIgnoreCount
                + players.IndexOf(players.First(p => p[(int)NAME_COLUMN].ToString() == playerName))
                + 1;

            var newValues = GetUpdatedValues(values, players);

            // Add empty rows to clear unnecesary data
            var endEmptyRows = 30;
            while (--endEmptyRows != 0)
                newValues.Add(new List<object> { string.Empty, string.Empty});

            await UpdateLastRowStyle(values.IndexOf(GetTotalsRow(values)), newValues.IndexOf(GetTotalsRow(values)));
            await UpdateSheetAsync(newValues, range);
            
            return newPlayerRowNumber;
        }

        private List<IList<object>> GetUpdatedValues(IList<IList<object>> values, IList<IList<object>> players)
        {
            var startRowsToIgnore = GetStartRows(values);
            var newValues = new List<IList<object>>(startRowsToIgnore);
            newValues.AddRange(players);

            var totalsRow = GetTotalsRow(values);
            var firstPlayerCell = $"{APPROVE_COLUMN}{startRowsToIgnore.Count() + 1}";
            var lastPlayerCell = $"{APPROVE_COLUMN}{startRowsToIgnore.Count() + players.Count}";

            totalsRow[(int)APPROVE_COLUMN] = $"=SUM({firstPlayerCell}:{lastPlayerCell})";
            newValues.Add(totalsRow);

            return newValues;
        }

        private IList<object> GetTotalsRow(IList<IList<object>> values)
        {
            var totalsRow = values.FirstOrDefault(v => CellEqualsValue(v, (int)NAME_COLUMN, TOTAL_LABEL));

            if (totalsRow == null)
                throw new TotalsRowNotFoundExeption();

            if (totalsRow.Count < 2)
                totalsRow.Add(string.Empty);

            return totalsRow;
        }

        private bool CellEqualsValue(IList<object> row, int columnIndex, string value)
        {
           return row.Count > columnIndex 
               && row[columnIndex]?.ToString().Trim().Equals(value, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        private async Task UpdateLastRowStyle(int oldTotalsRowIndex, int newTotalsRowIndex)
        {
            if (oldTotalsRowIndex == newTotalsRowIndex)
                return;

            var cellWithDefaultStyleRowIndex = 5;
            var copyStyleToTotalsRowRequest = GetCopyStyleRequest(oldTotalsRowIndex, newTotalsRowIndex);
            var copyStyleToOldTotalsRowRequest = GetCopyStyleRequest(cellWithDefaultStyleRowIndex, oldTotalsRowIndex);

            var cutPasteStyleRequest = new BatchUpdateSpreadsheetRequest()
            {
                Requests = new List<Request>
                {
                    new Request() { CopyPaste = copyStyleToTotalsRowRequest },
                    new Request() { CopyPaste = copyStyleToOldTotalsRowRequest }
                }
            };

            var updateRequest = _sheetsService.Spreadsheets.BatchUpdate(cutPasteStyleRequest, AppSettings.GoogleDocSheetId);
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await updateRequest.ExecuteAsync(cancellationToken);
        }

        private CopyPasteRequest GetCopyStyleRequest(int sourceRowIndex, int destinationRowIndex)
        {
            return new CopyPasteRequest()
            {
                Source = new GridRange()
                {
                    StartRowIndex = sourceRowIndex,
                    EndRowIndex = sourceRowIndex + 1,
                    StartColumnIndex = (int)NAME_COLUMN,
                    EndColumnIndex = (int)APPROVE_COLUMN + 1
                },
                Destination = new GridRange()
                {
                    StartRowIndex = destinationRowIndex,
                    EndRowIndex = destinationRowIndex + 1,
                    StartColumnIndex = (int)NAME_COLUMN,
                    EndColumnIndex = (int)APPROVE_COLUMN + 1
                },
                PasteType = "PASTE_FORMAT"
            };
        }

        private IList<IList<object>> GetOrderedPlayers(IEnumerable<IList<object>> values, int startRowsToIgnore, string newPlayerName = null)
        {
            var players = values
                .Skip(startRowsToIgnore)
                .Where(v => v.Count > 0 && !string.IsNullOrWhiteSpace(v[(int)NAME_COLUMN]?.ToString()))
                .TakeWhile(v => !CellEqualsValue(v, (int)NAME_COLUMN, TOTAL_LABEL))
                .ToList();

            if (newPlayerName != null)
                players.Add(new List<object> { newPlayerName, string.Empty });

            // Set empty string for approve cells. Null values do not clear cells.
            foreach (var player in players.Where(v => v.Skip((int)NAME_COLUMN + 1).FirstOrDefault() == null))
            {
                if (player.Count == 1)
                    player.Add(string.Empty);
                else
                    player[(int)APPROVE_COLUMN] = string.Empty;
            }

            return players.OrderBy(v => v[(int)NAME_COLUMN]).ToList();
        }

        private async Task<BatchUpdateValuesResponse> UpdateSheetAsync(IList<IList<object>> values, string range)
        {
            var data = new BatchUpdateValuesRequest()
            {
                Data = new List<ValueRange>()
                {
                    new ValueRange()
                    {
                        Range = range,
                        Values = values
                    }
                },
                ValueInputOption = "USER_ENTERED"
            };

            var request = _sheetsService.Spreadsheets.Values.BatchUpdate(data, AppSettings.GoogleDocSheetId);
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            return await request.ExecuteAsync(cancellationToken);
        }

        private ValueRange GetValueRange(string range, params object [] values)
        {
            return new ValueRange()
            {
                Range = range,
                Values = new List<IList<object>>() { new List<object>(values) }
            };
        }

        private IEnumerable<IList<object>> GetStartRows(IList<IList<object>> values)
        {
            return values.TakeWhile(v => v.Count == 0 || string.IsNullOrWhiteSpace(v[(int)NAME_COLUMN]?.ToString()));
        }
    }
}


using Google.Apis.Auth.OAuth2;
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
            var userRaw = GetUserRowNumber(playerName, sheet);

            if (userRaw == -1)
                userRaw = await CreateNewPlayerRowAsync(sheet.Values, sheet.Range, playerName);

            var range = $"{SHEET_NAME}!{APPROVE_COLUMN}{userRaw}";
            var dataValueRange = GetValueRange(range, cellValue);
                        
            var request = _sheetsService.Spreadsheets.Values.Update(dataValueRange, AppSettings.GoogleDocSheetId, range);
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
            var startRowsToIgnore = GetStartRows(sheet.Values);

            return GetOrderedPlayers(sheet.Values, startRowsToIgnore).Sum(p =>
            {
                if (p.Count < 2) return 0;
                double.TryParse(p[(int)APPROVE_COLUMN]?.ToString(), out double approveValue);
                return (int)approveValue;
            });
        }

        public async Task ClearApproveCellsAsync()
        {
            var sheet = await GetSheetAsync();
            var values = sheet.Values;
            var startRowsToIgnore = GetStartRows(values);

            var players = GetOrderedPlayers(values, startRowsToIgnore);
            
            var newValues = new List<IList<object>>();
            foreach (var player in players)
                newValues.Add(new List<object>() { string.Empty });

            var firstPlayerCell = $"{APPROVE_COLUMN}{startRowsToIgnore.Count() + 1}";
            var lastPlayerCell = $"{APPROVE_COLUMN}{startRowsToIgnore.Count() + players.Count}";

            await UpdateSheetAsync(newValues, $"{firstPlayerCell}:{lastPlayerCell}");
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
            var playerExists = values.Any(v => v.Count > 0 && v[(int)NAME_COLUMN]?.ToString().Trim().Equals(playerName, StringComparison.InvariantCultureIgnoreCase) == true);
            if (playerExists)
                throw new ArgumentException($"Player {playerName} already exists.");

            var startRowsToIgnore = GetStartRows(values);
            var totalsRow = values.FirstOrDefault(v => v.Count > 0 && v[(int)NAME_COLUMN]?.ToString().Trim().Equals(TOTAL_LABEL, StringComparison.InvariantCultureIgnoreCase) == true); // "Всего" row
            if (totalsRow == null)
                throw new TotalsRowNotFoundExeption();

            var players = GetOrderedPlayers(values, startRowsToIgnore, playerName);

            var newPlayerRowNumber = startRowsToIgnore.Count()
                + players.IndexOf(players.First(p => p[(int)NAME_COLUMN].ToString() == playerName))
                + 1;

            var newValues = new List<IList<object>>(startRowsToIgnore);
            newValues.AddRange(players);

            var firstPlayerCell = $"{APPROVE_COLUMN}{startRowsToIgnore.Count() + 1}";
            var lastPlayerCell = $"{APPROVE_COLUMN}{startRowsToIgnore.Count() + players.Count}";
            totalsRow[(int)APPROVE_COLUMN] = $"=SUM({firstPlayerCell}:{lastPlayerCell})";
            newValues.Add(totalsRow);

            await UpdateSheetAsync(newValues, range);
            await UpdateLastRowStyle(newValues.IndexOf(totalsRow));

            return newPlayerRowNumber;
        }

        private async Task UpdateLastRowStyle(int totalsRowIndex)
        {
            var copyStyleToTotalsRowRequest = new CopyPasteRequest()
            {
                Source = new GridRange() { StartRowIndex = totalsRowIndex - 1, StartColumnIndex = (int)NAME_COLUMN },
                Destination = new GridRange() { StartRowIndex = totalsRowIndex, StartColumnIndex = (int)NAME_COLUMN },
                PasteType = "PASTE_FORMAT"
            };

            var copyStyleToLastPlayerRowRequest = new CopyPasteRequest()
            {
                Source = new GridRange() { StartRowIndex = totalsRowIndex - 2, EndRowIndex = totalsRowIndex - 1, StartColumnIndex = (int)NAME_COLUMN },
                Destination = new GridRange() { StartRowIndex = totalsRowIndex - 1, EndRowIndex = totalsRowIndex, StartColumnIndex = (int)NAME_COLUMN },
                PasteType = "PASTE_FORMAT"
            };

            var cutPasteStyleRequest = new BatchUpdateSpreadsheetRequest()
            {
                Requests = new List<Request>
                {
                    new Request() { CopyPaste = copyStyleToTotalsRowRequest },
                    new Request() { CopyPaste = copyStyleToLastPlayerRowRequest }
                }
            };

            var updateRequest = _sheetsService.Spreadsheets.BatchUpdate(cutPasteStyleRequest, AppSettings.GoogleDocSheetId);
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            await updateRequest.ExecuteAsync(cancellationToken);
        }

        private IList<IList<object>> GetOrderedPlayers(IEnumerable<IList<object>> values, IEnumerable<IList<object>> startRowsToIgnore, string newPlayerName = null)
        {
            var players = values
                .Skip(startRowsToIgnore.Count())
                .Where(v => v.Count > 0 && !string.IsNullOrWhiteSpace(v[(int)NAME_COLUMN]?.ToString()))
                .TakeWhile(v => !v[(int)NAME_COLUMN].ToString().Trim().Equals(TOTAL_LABEL, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            if (newPlayerName != null)
            {
                var newPlayerRow = new List<object> { newPlayerName, string.Empty };
                players.Add(newPlayerRow);
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


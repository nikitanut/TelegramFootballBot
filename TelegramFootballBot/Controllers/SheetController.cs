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

        public async Task<string> UpdateApproveCellAsync(int userId, string cellValue)
        {
            var player = Bot.Players.FirstOrDefault(p => p.Id == userId);
            if (player == null)
                throw new UserNotFoundException();

            var sheet = await GetSheetAsync();
            var userRaw = GetUserRowNumber(player.Name, sheet);

            if (userRaw == -1)
                userRaw = await CreateNewPlayerRowAsync(sheet.Values, sheet.Range, player.Name);

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
            {
                userRaw = await CreateNewPlayerRowAsync(sheet.Values, sheet.Range, playerName);
                var range = $"{SHEET_NAME}!{NAME_COLUMN}{userRaw}";
                var dataValueRange = GetValueRange(range, playerName);

                var request = _sheetsService.Spreadsheets.Values.Update(dataValueRange, AppSettings.GoogleDocSheetId, range);
                request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
                var response = await request.ExecuteAsync(cancellationToken);
            }
        }

        public async Task<int> GetTotalApprovedPlayersAsync()
        {
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            var sheet = await GetSheetAsync();
            var startRowsToIgnore = GetStartRows(sheet.Values);

            return GetOrderedPlayers(sheet.Values, startRowsToIgnore).Sum(p =>
            {
                if (p.Count < 2) return 0;
                double.TryParse(p[(int)APPROVE_COLUMN]?.ToString(), out double approveValue);
                return (int)approveValue;
            });
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
            var range = SHEET_NAME;
            var request = _sheetsService.Spreadsheets.Values.Get(AppSettings.GoogleDocSheetId, range);
            var cancellationToken = new CancellationTokenSource(Constants.ASYNC_OPERATION_TIMEOUT).Token;
            return await request.ExecuteAsync(cancellationToken);
        }

        private async Task<int> CreateNewPlayerRowAsync(IList<IList<object>> values, string range, string playerName)
        {
            var playerExists = values.Any(v => v.Count > 0 && v[(int)NAME_COLUMN]?.ToString().Equals(playerName, StringComparison.InvariantCultureIgnoreCase) == true);
            if (playerExists)
                throw new ArgumentException($"Player {playerName} already exists.");

            var startRowsToIgnore = GetStartRows(values);
            var totalsRow = values.FirstOrDefault(v => v.Count > 0 && v[(int)NAME_COLUMN]?.ToString().Equals(TOTAL_LABEL, StringComparison.InvariantCultureIgnoreCase) == true); // "Всего" row
            if (totalsRow == null)
                throw new TotalsRowNotFoundExeption();

            var players = GetOrderedPlayers(values, startRowsToIgnore, playerName);

            var newPlayerRowNumber = startRowsToIgnore.Count() 
                + players.IndexOf(players.First(p => p[(int)NAME_COLUMN].ToString() == playerName)) 
                + 1;

            var newValues = new List<IList<object>>(startRowsToIgnore);
            newValues.AddRange(players);
            newValues.Add(totalsRow);

            // TODO: Repair totals row (becomes last but one, formula moves up)
            await UpdateSheetAsync(newValues, range);
            return newPlayerRowNumber;
        }

        private IList<IList<object>> GetOrderedPlayers(IEnumerable<IList<object>> values, IEnumerable<IList<object>> startRowsToIgnore, string newPlayerName = null)
        {
            var players = values
                .Skip(startRowsToIgnore.Count())
                .Where(v => v.Count > 0 && !string.IsNullOrWhiteSpace(v[(int)NAME_COLUMN]?.ToString()))
                .TakeWhile(v => !v[(int)NAME_COLUMN].ToString().Equals(TOTAL_LABEL, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            if (newPlayerName != null)
            {
                var newPlayerRow = new List<object> { newPlayerName };
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


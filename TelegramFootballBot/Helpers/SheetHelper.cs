using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using TelegramFootballBot.Models;

namespace TelegramFootballBot.Helpers
{
    public static class SheetHelper
    {
        private enum Column { A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z }

        private const Column NAME_COLUMN = Column.A;
        private const Column APPROVE_COLUMN = Column.B;

        private const string SHEET_NAME = "Участие в играх";
        private const string TOTAL_LABEL = "Всего";
        private const int DEFAULT_STYLE_ROW_INDEX = 5;

        private static readonly Dictionary<int, string> _monthNames = new Dictionary<int, string>()
        {
            { 1, "января" }, { 2, "февраля" }, { 3, "марта" }, { 4, "апреля" }, { 5, "мая" }, { 6, "июня" },
            { 7, "июля" }, { 8, "августа" }, { 9, "сентября" }, { 10, "октября" }, { 11, "ноября" }, { 12, "декабря" }
        };

        public static IEnumerable<IList<object>> GetStartRows(IList<IList<object>> values)
        {
            return values.TakeWhile(v => v.Count == 0 || string.IsNullOrWhiteSpace(v[(int)NAME_COLUMN]?.ToString()));
        }

        public static ValueRange GetValueRange(string range, params object[] values)
        {
            return new ValueRange()
            {
                Range = range,
                Values = new List<IList<object>>() { new List<object>(values) }
            };
        }

        public static List<IList<object>> GetNewValues(IList<IList<object>> values, IList<IList<object>> players)
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

        public static IList<object> GetTotalsRow(IList<IList<object>> values)
        {
            var totalsRow = values.FirstOrDefault(v => CellEqualsValue(v, (int)NAME_COLUMN, TOTAL_LABEL));

            if (totalsRow == null)
                throw new TotalsRowNotFoundExeption();

            if (totalsRow.Count < 2)
                totalsRow.Add(string.Empty);

            return totalsRow;
        }

        public static IList<IList<object>> GetOrderedPlayers(IList<IList<object>> values, string newPlayerName = null)
        {
            var startRowsToIgnore = GetStartRows(values).Count();

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

        public static string GetApprovedPlayersString(IList<IList<object>> players)
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

        public static int GetTotalApprovedPlayers(IList<IList<object>> players)
        {
            return players.Sum(p =>
            {
                if (p.Count <= (int)APPROVE_COLUMN) return 0;
                double.TryParse(p[(int)APPROVE_COLUMN]?.ToString(), out double approveValue);
                return (int)approveValue;
            });
        }

        public static int GetUserRowNumber(IList<IList<object>> values, string playerName)
        {
            if (playerName == null)
                return -1;

            var userRow = values.FirstOrDefault(v => v.Count > 0 
                && playerName.Equals(v[(int)NAME_COLUMN].ToString().Trim(), StringComparison.InvariantCultureIgnoreCase));

            var userRowIndex = values.IndexOf(userRow);
            return userRowIndex != -1 ? userRowIndex + 1 : -1;
        }

        public static BatchUpdateSpreadsheetRequest GetMoveCellsStyleRequest(int sourceRowIndex, int destinationRowIndex)
        {
            return new BatchUpdateSpreadsheetRequest()
            {
                Requests = new List<Request>
                {
                    new Request() { CopyPaste = GetCopyStyleRequest(sourceRowIndex, destinationRowIndex) },
                    new Request() { CopyPaste = GetCopyStyleRequest(DEFAULT_STYLE_ROW_INDEX, sourceRowIndex) }
                }
            };
        }

        public static string GetDateWithRussianMonth(DateTime date)
        {
            return $"{date.Day} {_monthNames[date.Month]}";
        }

        public static string GetApproveColumnRange(IList<IList<object>> values, int totalPlayers)
        {
            var startRowsToIgnore = GetStartRows(values).Count();
            var dateOfGameCell = $"{APPROVE_COLUMN}{startRowsToIgnore}";
            var lastPlayerCell = $"{APPROVE_COLUMN}{startRowsToIgnore + totalPlayers}";
            return $"{dateOfGameCell}:{lastPlayerCell}";
        }

        public static string GetUserRange(int userRow)
        {
            return $"{SHEET_NAME}!{APPROVE_COLUMN}{userRow}";
        }

        public static string GetAllUsersRange()
        {
            return $"{SHEET_NAME}!{NAME_COLUMN}:{APPROVE_COLUMN}";
        }
        
        private static bool CellEqualsValue(IList<object> row, int columnIndex, string value)
        {
            return row.Count > columnIndex
                && row[columnIndex]?.ToString().Trim().Equals(value, StringComparison.InvariantCultureIgnoreCase) == true;
        }

        private static CopyPasteRequest GetCopyStyleRequest(int sourceRowIndex, int destinationRowIndex)
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

        public static BatchUpdateValuesRequest GetBatchUpdateRequest(IList<IList<object>> values, string range)
        {
            return new BatchUpdateValuesRequest()
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
        }
    }
}

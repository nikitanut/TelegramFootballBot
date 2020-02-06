using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TelegramFootballBot.Controllers;
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
        private const int START_ROWS_COUNT = 2;
        
        public static IEnumerable<IList<object>> GetStartRows(IList<IList<object>> values)
        {
            return values.Take(START_ROWS_COUNT);
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

            // Set empty string for approve cells to clear data
            foreach (var player in players.Where(v => v.Skip((int)NAME_COLUMN + 1).FirstOrDefault() == null))
            {
                if (player.Count <= (int)APPROVE_COLUMN)
                    player.Add(string.Empty);
                else
                    player[(int)APPROVE_COLUMN] = string.Empty;
            }

            return players.OrderBy(v => v[(int)NAME_COLUMN]).ToList();
        }

        public static string GetApprovedPlayersString(IList<IList<object>> players)
        {            
            var headerMessage = $"{Scheduler.GetNearestGameDateMoscowTime(DateTime.UtcNow).ToRussianDayMonthString()}. Отметились: {GetTotalApprovedPlayers(players)}.";
            var markedPlayers = GetMarkedPlayers(players);

            var playersMessage = new StringBuilder(headerMessage);
            playersMessage.AppendLine();
            playersMessage.AppendLine(GetDashedString(headerMessage.Length));
            playersMessage.AppendLine(string.Join(Environment.NewLine, markedPlayers.Where(p => p.Value == '+').Select(p => p.Key)));

            if (GetTotalMaybePlayers(players) > 0)
            {
                playersMessage.AppendLine(GetDashedString(headerMessage.Length));
                playersMessage.AppendLine($"Под вопросом: {GetTotalMaybePlayers(players)}.");
                playersMessage.AppendLine(string.Join(Environment.NewLine, markedPlayers.Where(p => p.Value == '?').Select(p => p.Key)));
            }

            return playersMessage.ToString();
        }

        private static IEnumerable<KeyValuePair<string, char>> GetMarkedPlayers(IList<IList<object>> players)
        {
            return players.Where(p =>
            {
                if (p.Count <= (int)APPROVE_COLUMN) return false;
                var approveValue = ToDouble(p[(int)APPROVE_COLUMN]);
                return approveValue > 0;
            })
            .Select(p =>
            {
                var playerName = p[(int)NAME_COLUMN].ToString();
                var countByPlayer = ToDouble(p[(int)APPROVE_COLUMN]);
                if (countByPlayer == 1) return new KeyValuePair<string, char>(playerName, '+');
                if (countByPlayer < 1) return new KeyValuePair<string, char>(playerName, '?');
                return new KeyValuePair<string, char>($"{playerName} x{countByPlayer}", '+');
            });
        }

        private static int GetTotalApprovedPlayers(IList<IList<object>> players)
        {
            return players.Sum(p =>
            {
                if (p.Count <= (int)APPROVE_COLUMN) return 0;
                var approveValue = ToDouble(p[(int)APPROVE_COLUMN]);
                return (int)approveValue;
            });
        }

        private static int GetTotalMaybePlayers(IList<IList<object>> players)
        {
            return players.Sum(p =>
            {
                if (p.Count <= (int)APPROVE_COLUMN) return 0;
                var approveValue = ToDouble(p[(int)APPROVE_COLUMN]);
                return approveValue % 1 != 0 ? 1 : 0;
            });
        }

        private static double ToDouble(object cell)
        {
            double.TryParse(cell?.ToString().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double value);
            return value;
        }

        public static int GetUserRowNumber(IList<IList<object>> values, string playerName)
        {
            if (playerName == null)
                return -1;

            var userRow = values.FirstOrDefault(v => v.Count > 0 
                && playerName.Equals(v[(int)NAME_COLUMN]?.ToString().Trim(), StringComparison.InvariantCultureIgnoreCase));

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
                    new Request() { CopyPaste = GetCopyStyleRequest(DEFAULT_STYLE_ROW_INDEX, sourceRowIndex) },
                    new Request() { UpdateDimensionProperties = GetStandardRowHeightRequest(destinationRowIndex) }
                }
            };
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

        public static string GetDashedString(int mainStringLength)
        {
            return new string('-', 30);
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

        private static UpdateDimensionPropertiesRequest GetStandardRowHeightRequest(int destinationRowIndex)
        {
            return new UpdateDimensionPropertiesRequest()
            {
                Range = new DimensionRange()
                {
                    Dimension = "ROWS",
                    StartIndex = destinationRowIndex,
                    EndIndex = destinationRowIndex + 1
                },
                Properties = new DimensionProperties() { PixelSize = 30 },
                Fields = "pixelSize"
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

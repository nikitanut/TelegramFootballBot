using Google.Apis.Sheets.v4.Data;
using System.Globalization;
using System.Text;
using TelegramFootballBot.Core.Exceptions;

namespace TelegramFootballBot.Core.Helpers
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

        public static IEnumerable<IList<object>> GetHeaderRows(IList<IList<object>> values)
        {
            return values.Take(START_ROWS_COUNT);
        }

        public static ValueRange ToValueRange(string range, params object[] values)
        {
            return new ValueRange()
            {
                Range = range,
                Values = new List<IList<object>>() { new List<object>(values) }
            };
        }

        public static List<IList<object>> ApplyPlayers(IList<IList<object>> currentSheet, IList<IList<object>> players)
        {
            var rowsToIgnore = GetHeaderRows(currentSheet);
            var updatedSheet = new List<IList<object>>(rowsToIgnore);
            updatedSheet.AddRange(players);

            var totalsRow = GetTotalsRow(currentSheet);
            var firstPlayerCell = $"{APPROVE_COLUMN}{rowsToIgnore.Count() + 1}";
            var lastPlayerCell = $"{APPROVE_COLUMN}{rowsToIgnore.Count() + players.Count}";

            totalsRow[(int)APPROVE_COLUMN] = $"=SUM({firstPlayerCell}:{lastPlayerCell})";
            updatedSheet.Add(totalsRow);

            return updatedSheet;
        }

        public static IList<object> GetTotalsRow(IList<IList<object>> values)
        {
            var totalsRow = values.FirstOrDefault(v => AreEqual(v, (int)NAME_COLUMN, TOTAL_LABEL)) ?? throw new TotalsRowNotFoundExeption();
            if (totalsRow.Count < 2)
                totalsRow.Add(string.Empty);

            return totalsRow;
        }

        public static IList<IList<object>> GetOrderedPlayers(IList<IList<object>> values, string newPlayerName = "")
        {
            var rowsToIgnore = GetHeaderRows(values).Count();

            var players = values
                .Skip(rowsToIgnore)
                .Where(v => v.Any() && !string.IsNullOrWhiteSpace(v[(int)NAME_COLUMN]?.ToString()))
                .TakeWhile(v => !AreEqual(v, (int)NAME_COLUMN, TOTAL_LABEL))
                .ToList();

            if (!string.IsNullOrEmpty(newPlayerName))
                players.Add(new List<object> { newPlayerName, string.Empty });

            var rowsWithEmptyApproveColumn = players.Where(v => v.Skip((int)NAME_COLUMN + 1).FirstOrDefault() == null);
            foreach (var playerRow in rowsWithEmptyApproveColumn)
            {
                if (playerRow.Count <= (int)APPROVE_COLUMN)
                    playerRow.Add(string.Empty);
                else
                    playerRow[(int)APPROVE_COLUMN] = string.Empty;
            }

            players.Sort((a, b) => a[(int)NAME_COLUMN].ToString()!.CompareTo(b[(int)NAME_COLUMN].ToString()));
            return players;
        }

        public static string BuildPlayersListMessage(IList<IList<object>> players)
        {
            var headerMessage = $"{DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow).ToRussianDayMonthString()}. Отметились: {CountPlayersReadyToGo(players)}.";
            var likelyToGoPlayers = FilterPlayersLikelyToGo(players);

            var playersMessage = new StringBuilder(headerMessage);
            playersMessage.AppendLine();
            playersMessage.AppendLine(MarkupHelper.DashedString);
            playersMessage.AppendLine(string.Join(Environment.NewLine, likelyToGoPlayers.Where(p => p.Value == '+').Select(p => p.Key)));

            var notSurePlayers = CountNotSurePlayers(players);
            if (notSurePlayers > 0)
            {
                playersMessage.AppendLine(MarkupHelper.DashedString);
                playersMessage.AppendLine($"Под вопросом: {notSurePlayers}.");
                playersMessage.AppendLine(string.Join(Environment.NewLine, likelyToGoPlayers.Where(p => p.Value == '?').Select(p => p.Key)));
            }

            return playersMessage.ToString();
        }

        public static List<string> GetPlayersReadyToPlay(IList<IList<object>> values)
        {
            return GetOrderedPlayers(values).Where(p =>
            {
                if (p.Count <= (int)APPROVE_COLUMN) return false;
                var approveValue = ToDouble(p[(int)APPROVE_COLUMN]);
                return approveValue >= 1;
            })
            .Select(p => p[(int)NAME_COLUMN].ToString()!.Trim())
            .ToList();
        }

        private static IEnumerable<KeyValuePair<string, char>> FilterPlayersLikelyToGo(IList<IList<object>> players)
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
                if (countByPlayer == 1) return new KeyValuePair<string, char>(playerName!, '+');
                if (countByPlayer < 1) return new KeyValuePair<string, char>(playerName!, '?');
                return new KeyValuePair<string, char>($"{playerName} x{countByPlayer}", '+');
            });
        }

        private static int CountPlayersReadyToGo(IList<IList<object>> players)
        {
            return players.Sum(p =>
            {
                if (p.Count <= (int)APPROVE_COLUMN) return 0;
                var approveValue = ToDouble(p[(int)APPROVE_COLUMN]);
                return (int)approveValue;
            });
        }

        private static int CountNotSurePlayers(IList<IList<object>> players)
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
            double.TryParse(cell?.ToString()!.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double value);
            return value;
        }

        public static int GetPlayerRowNumber(IList<IList<object>> values, string playerName)
        {
            if (playerName == null)
                return -1;

            var userRow = values.FirstOrDefault(v => v.Count > 0
                && playerName.Equals(v[(int)NAME_COLUMN]?.ToString()!.Trim(), StringComparison.InvariantCultureIgnoreCase));

            var userRowIndex = userRow is not null ? values.IndexOf(userRow) : -1;
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
            var startRowsToIgnore = GetHeaderRows(values).Count();
            var dateOfGameCell = $"{APPROVE_COLUMN}{startRowsToIgnore}";
            var lastPlayerCell = $"{APPROVE_COLUMN}{startRowsToIgnore + totalPlayers}";
            return $"{dateOfGameCell}:{lastPlayerCell}";
        }

        public static string GetPlayerRange(int playerRowNumber)
        {
            return $"{SHEET_NAME}!{APPROVE_COLUMN}{playerRowNumber}";
        }

        public static string GetAllPlayersRange()
        {
            return $"{SHEET_NAME}!{NAME_COLUMN}:{APPROVE_COLUMN}";
        }

        private static bool AreEqual(IList<object> row, int columnIndex, string value)
        {
            return row.Count > columnIndex
                && row[columnIndex]?.ToString()!.Trim().Equals(value, StringComparison.InvariantCultureIgnoreCase) == true;
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

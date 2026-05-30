using Google.Apis.Sheets.v4.Data;
using System.Globalization;
using System.Text;
using TelegramFootballBot.App.Exceptions;

namespace TelegramFootballBot.App.Helpers;

public static class SheetHelper
{
    private enum Column { A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z }

    private const Column _nameColumn = Column.A;
    private const Column _approveColumn = Column.B;

    private const string _sheetName = "Участие в играх";
    private const string _totalLable = "Всего";
    private const int _defaultStyleRawIndex = 5;
    private const int _startRowsCount = 2;

    public static IEnumerable<IList<object>> GetHeaderRows(IList<IList<object>> values)
    {
        return values.Take(_startRowsCount);
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
        var firstPlayerCell = $"{_approveColumn}{rowsToIgnore.Count() + 1}";
        var lastPlayerCell = $"{_approveColumn}{rowsToIgnore.Count() + players.Count}";

        totalsRow[(int)_approveColumn] = $"=SUM({firstPlayerCell}:{lastPlayerCell})";
        updatedSheet.Add(totalsRow);

        return updatedSheet;
    }

    public static IList<object> GetTotalsRow(IList<IList<object>> values)
    {
        var totalsRow = values.FirstOrDefault(v => AreEqual(v, (int)_nameColumn, _totalLable)) ?? throw new TotalsRowNotFoundExeption();
        if (totalsRow.Count < 2)
            totalsRow.Add(string.Empty);

        return totalsRow;
    }

    public static IList<IList<object>> GetOrderedPlayers(IList<IList<object>> values, string newPlayerName = "")
    {
        var rowsToIgnore = GetHeaderRows(values).Count();

        var players = values
            .Skip(rowsToIgnore)
            .Where(v => v.Any() && !string.IsNullOrWhiteSpace(v[(int)_nameColumn]?.ToString()))
            .TakeWhile(v => !AreEqual(v, (int)_nameColumn, _totalLable))
            .ToList();

        if (!string.IsNullOrEmpty(newPlayerName))
            players.Add(new List<object> { newPlayerName, string.Empty });

        var rowsWithEmptyApproveColumn = players.Where(v => v.Skip((int)_nameColumn + 1).FirstOrDefault() is null);
        foreach (var playerRow in rowsWithEmptyApproveColumn)
        {
            if (playerRow.Count <= (int)_approveColumn)
                playerRow.Add(string.Empty);
            else
                playerRow[(int)_approveColumn] = string.Empty;
        }

        players.Sort((a, b) => a[(int)_nameColumn].ToString()!.CompareTo(b[(int)_nameColumn].ToString()));
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
            if (p.Count <= (int)_approveColumn) return false;
            var approveValue = ToDouble(p[(int)_approveColumn]);
            return approveValue >= 1;
        })
        .Select(p => p[(int)_nameColumn].ToString()!.Trim())
        .ToList();
    }

    private static IEnumerable<KeyValuePair<string, char>> FilterPlayersLikelyToGo(IList<IList<object>> players)
    {
        return players.Where(p =>
        {
            if (p.Count <= (int)_approveColumn) return false;
            var approveValue = ToDouble(p[(int)_approveColumn]);
            return approveValue > 0;
        })
        .Select(p =>
        {
            var playerName = p[(int)_nameColumn].ToString();
            var countByPlayer = ToDouble(p[(int)_approveColumn]);
            if (countByPlayer == 1) return new KeyValuePair<string, char>(playerName!, '+');
            if (countByPlayer < 1) return new KeyValuePair<string, char>(playerName!, '?');
            return new KeyValuePair<string, char>($"{playerName} x{countByPlayer}", '+');
        });
    }

    private static int CountPlayersReadyToGo(IList<IList<object>> players)
    {
        return players.Sum(p =>
        {
            if (p.Count <= (int)_approveColumn) return 0;
            var approveValue = ToDouble(p[(int)_approveColumn]);
            return (int)approveValue;
        });
    }

    private static int CountNotSurePlayers(IList<IList<object>> players)
    {
        return players.Sum(p =>
        {
            if (p.Count <= (int)_approveColumn) return 0;
            var approveValue = ToDouble(p[(int)_approveColumn]);
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
        if (playerName is null)
            return -1;

        var userRow = values.FirstOrDefault(v => v.Count > 0
            && playerName.Equals(v[(int)_nameColumn]?.ToString()?.Trim(), StringComparison.InvariantCultureIgnoreCase));

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
                new Request() { CopyPaste = GetCopyStyleRequest(_defaultStyleRawIndex, sourceRowIndex) },
                new Request() { UpdateDimensionProperties = GetStandardRowHeightRequest(destinationRowIndex) }
            }
        };
    }

    public static string GetApproveColumnRange(IList<IList<object>> values, int totalPlayers)
    {
        var startRowsToIgnore = GetHeaderRows(values).Count();
        var dateOfGameCell = $"{_approveColumn}{startRowsToIgnore}";
        var lastPlayerCell = $"{_approveColumn}{startRowsToIgnore + totalPlayers}";
        return $"{dateOfGameCell}:{lastPlayerCell}";
    }

    public static string GetPlayerRange(int playerRowNumber)
    {
        return $"{_sheetName}!{_approveColumn}{playerRowNumber}";
    }

    public static string GetAllPlayersRange()
    {
        return $"{_sheetName}!{_nameColumn}:{_approveColumn}";
    }

    private static bool AreEqual(IList<object> row, int columnIndex, string value)
    {
        return row.Count > columnIndex
            && row[columnIndex]?.ToString()?.Trim().Equals(value, StringComparison.InvariantCultureIgnoreCase) == true;
    }

    private static CopyPasteRequest GetCopyStyleRequest(int sourceRowIndex, int destinationRowIndex)
    {
        return new CopyPasteRequest()
        {
            Source = new GridRange()
            {
                StartRowIndex = sourceRowIndex,
                EndRowIndex = sourceRowIndex + 1,
                StartColumnIndex = (int)_nameColumn,
                EndColumnIndex = (int)_approveColumn + 1
            },
            Destination = new GridRange()
            {
                StartRowIndex = destinationRowIndex,
                EndRowIndex = destinationRowIndex + 1,
                StartColumnIndex = (int)_nameColumn,
                EndColumnIndex = (int)_approveColumn + 1
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
            Data =
            [
                new ValueRange()
                {
                    Range = range,
                    Values = values
                }
            ],
            ValueInputOption = "USER_ENTERED"
        };
    }
}

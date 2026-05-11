using TelegramFootballBot.App.Helpers;

namespace TelegramFootballBot.App.Models.CallbackQueries;

public class PlayerSetCallback(string callbackData) : Callback(callbackData)
{
    public static string Name => "PlayersSetDetermination";

    public DateTime GameDate { get; private set; } = ParseGameDate(callbackData);

    public static string BuildCallbackPrefix(DateTime gameDate)
    {
        return Name + Constants.CALLBACK_DATA_SEPARATOR + gameDate.ToString("yyyy-MM-dd");
    }

    private static DateTime ParseGameDate(string callbackData)
    {
        var gameDateString = Prefix(callbackData).Split(Constants.CALLBACK_DATA_SEPARATOR).Last();
        return DateTime.TryParse(gameDateString, out DateTime gameDate)
            ? gameDate
            : throw new ArgumentException($"Game date was not provided for callback data: {callbackData}", nameof(callbackData));
    }
}

namespace TelegramFootballBot.App;

public static class AppData
{
    public static string? DataDirectory =>
        Environment.GetEnvironmentVariable("DATA_DIR") is { Length: > 0 } dir ? dir : null;

    public static string File(string fileName)
    {
        if (DataDirectory is not { } dataDirectory)
            return fileName;

        Directory.CreateDirectory(dataDirectory);
        return Path.Combine(dataDirectory, fileName);
    }

    public static string DatabasePath()
    {
        var path = File("BotDb.db");
        if (DataDirectory is not null && !System.IO.File.Exists(path))
            throw new FileNotFoundException($"Copy BotDb.db into {DataDirectory} before starting the bot.", path);

        return path;
    }
}

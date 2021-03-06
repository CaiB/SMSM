namespace SMSMService;

public static class Log
{
    private static string ServerName = "Minecraft Server";
    public static StreamWriter? LogFile;
    public static void Server(string message)
    {
        string FormattedMsg = $"[SRV][{ServerName}] {message}";
        Console.WriteLine(FormattedMsg);
        // TODO: Output this elsewhere? Remote conn?
    }
    public static void Info(string message)
    {
        string FormattedMsg = $"[INF][{ServerName}] {message}";
        Console.WriteLine(FormattedMsg);
        if (LogFile != null) { LogFile.WriteLine(FormattedMsg); }
        Handlers?.Invoke(FormattedMsg);
    }
    public static void Warn(string message)
    {
        string FormattedMsg = $"[WRN][{ServerName}] {message}";
        Console.WriteLine(FormattedMsg);
        if (LogFile != null) { LogFile.WriteLine(FormattedMsg); }
        Handlers?.Invoke(FormattedMsg);
    }
    public static void Error(string message)
    {
        string FormattedMsg = $"[ERR][{ServerName}] {message}";
        Console.WriteLine(FormattedMsg);
        if (LogFile != null) { LogFile.WriteLine(FormattedMsg); }
        Handlers?.Invoke(FormattedMsg);
    }

    public static void SetName(string newName) => ServerName = newName;

    public delegate void LogHandler(string message);
    public static event LogHandler? Handlers;
}


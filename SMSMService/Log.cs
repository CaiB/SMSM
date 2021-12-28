namespace SMSMService;

public static class Log
{
    private static string ServerName = "Minecraft Server";
    public static void Info(string message) { Console.WriteLine($"[INF][{ServerName}] {message}"); }
    public static void Warn(string message) { Console.WriteLine($"[WRN][{ServerName}] {message}"); }
    public static void Error(string message) { Console.WriteLine($"[ERR][{ServerName}] {message}"); }

    public static void SetName(string newName) => ServerName = newName;
}


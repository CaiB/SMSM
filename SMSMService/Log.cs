namespace SMSMService
{
    public static class Log
    {
        public static void Info(string message) { Console.WriteLine($"[INF] {message}"); }
        public static void Warn(string message) { Console.WriteLine($"[WRN] {message}"); }
        public static void Error(string message) { Console.WriteLine($"[ERR] {message}"); }

    }
}

using SMSMService.Tasks;

namespace SMSMService;
public static class SMSM
{
    public const bool USE_NOGUI = false; // Only used for debugging purposes.
    public static string? JavaPath;
    public static string? JavaArgs;
    public static string? ServerDir;

    public static void Main(string[] args)
    {
        bool ConfigResult = ConfigReader.ReadConfig("config.json");
        if (ConfigResult)
        {
            Server.StartServer();
        }
        
        while (true)
        {
            char Key = Console.ReadKey().KeyChar;
            if (Key == 'e') { new StopServer().DoTask(null); }
            if (Key == 'r') { new RestartServer().DoTask(null); }
            if (Key == 's') { new SaveWorld().DoTask(null); }
            if (Key == 'b') { new CreateBackup().DoTask(null); }
            if (Key == 'x') { break; }
        }
    }
    
}
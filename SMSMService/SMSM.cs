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
        TaskHandler.AddTasks();

        bool ConfigResult = ConfigReader.ReadConfig("config.json");
        if (!ConfigResult)
        {
            Log.Error("The configuration could not be parsed, and SMSM will now exit.");
            Environment.Exit(-2);
        }

        Scheduler.Start();
        
        while (true)
        {
            char Key = Console.ReadKey().KeyChar;
            if (Key == 'w') { TaskHandler.Tasks["start"].Invoke(null); }
            if (Key == 'e') { TaskHandler.Tasks["stop"].Invoke(null); }
            if (Key == 'r') { TaskHandler.Tasks["restart"].Invoke(null); }
            if (Key == 's') { TaskHandler.Tasks["save"].Invoke(null); }
            if (Key == 'b') { TaskHandler.Tasks["backup"].Invoke(null); }
            if (Key == 'x') { break; }
        }
    }
    
}
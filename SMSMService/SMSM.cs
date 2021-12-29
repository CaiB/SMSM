using SMSMService.Tasks;

namespace SMSMService;
public class SMSM : BackgroundService
{
    public const bool USE_NOGUI = false; // Only used for debugging purposes.
    public static string? JavaPath;
    public static string? JavaArgs;
    public static string? ServerDir;
    public static string ServerName = "No Name";

    private readonly ILogger<SMSM> Logger;

    public SMSM(ILogger<SMSM> logger)
    {
        this.Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TaskHandler.AddTasks();

        bool ConfigResult = ConfigReader.ReadConfig("config.json");
        if (!ConfigResult)
        {
            Log.Error("The configuration could not be parsed, and SMSM will now exit.");
            Environment.Exit(-2);
        }
        BackupTask.Init();

        Scheduler.Start();

        RemoteConnector.Start(ServerName);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public static void Stop()
    {
        Log.Info("SMSM exit requested.");
        Server.SendInput("/stop");
        RemoteConnector.Stop();
        Scheduler.Stop();
    }
    
}
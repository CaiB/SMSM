using Microsoft.Extensions.Hosting.WindowsServices;
using SMSMService.Tasks;

namespace SMSMService;
public class SMSM : BackgroundService
{
    public const bool USE_NOGUI = true; // Only disabled for debugging purposes.
    public static string? ConfigFile;
    public static string? JavaPath;
    public static string? JavaArgs;
    public static string? ServerDir;
    public static string ServerName = "No Name";
    public static bool AutoStart = true;

    private readonly ILogger<SMSM> Logger;
    private readonly IHostApplicationLifetime Lifetime;

    public SMSM(ILogger<SMSM> logger, IHostApplicationLifetime lifetime)
    {
        this.Logger = logger;
        this.Lifetime = lifetime;
        this.Lifetime.ApplicationStopping.Register(Stop);
        if (this.Lifetime is WindowsServiceLifetime ServiceLife)
        {
            ServiceLife.CanStop = true;
            ServiceLife.CanShutdown = true;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await Task.Run(async () =>
    {
        try
        {
            TaskHandler.AddTasks();

            if (ConfigFile == null) { return; }
            bool ConfigResult = ConfigReader.ReadConfig(ConfigFile);
            if (!ConfigResult)
            {
                Log.Error("The configuration could not be parsed, and SMSM will now exit.");
                Environment.Exit(-2);
            }
            BackupTask.Init();

            Scheduler.Start();

            RemoteConnector.Start(ServerName);

            if (AutoStart)
            {
                bool Started = Server.StartServer();
                if (!Started) { Log.Error("Server could not be auto-started."); }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception Exc) { Log.Error(Exc.ToString()); }
        finally { Stop(); }
    });

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        Stop();
        await base.StopAsync(stoppingToken);
    }

    public static void Stop()
    {
        Log.Info("SMSM exit requested.");
        Server.SendInput("/say Server shutting down because the management service is stopping");
        Server.SendInput("/save-all");
        Server.SendInput("/stop");
        Server.WaitForExit();
        RemoteConnector.Stop();
        Scheduler.Stop();
        Log.Info("SMSM exit completed.");
    }

    public static void OnShutdown() => Stop();

}
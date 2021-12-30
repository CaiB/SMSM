namespace SMSMService;

public static class Start
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 1) { Console.WriteLine("Please specify the config file path as command-line argument."); Environment.Exit(-3); }
        SMSM.ConfigFile = args[0];

        string LogDate = DateTime.Now.ToString("yyyy-MM-dd\\_HH-mm-ss");
        try
        {
            string ExePath = AppDomain.CurrentDomain.BaseDirectory ?? "";
            string LogFileName = $"SMSMLog-{Environment.UserName}-{LogDate}.txt";
            string LogFilePath = Path.Combine(ExePath, LogFileName);
            Log.Info($"Attempting to log to \"{LogFilePath}\"");
            Log.LogFile = new StreamWriter(File.Create(LogFilePath)) { AutoFlush = true };
        }
        catch (Exception Exc)
        {
            Log.Error("Could not create log file.");
            Log.Error(Exc.ToString());
        }

        //Console.TreatControlCAsInput = true;

        IHost host = Host.CreateDefaultBuilder(args)
        .UseWindowsService(options =>
        {
            options.ServiceName = "SMSM Minecraft Server";
        })
        .ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(60))
        .ConfigureServices(services =>
        {
            services.AddHostedService<SMSM>();
        })
        .Build();

        await host.RunAsync();

        SMSM.Stop();
    }
}


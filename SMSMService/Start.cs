namespace SMSMService;

public static class Start
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 1) { Console.WriteLine("Please specify the config file path as command-line argument."); Environment.Exit(-3); }
        SMSM.ConfigFile = args[0];
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


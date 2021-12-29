namespace SMSMService;

public static class Start
{
    public static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
        .UseWindowsService(options =>
        {
            options.ServiceName = "SMSM Minecraft Server";
        })
        .ConfigureServices(services =>
        {
            services.AddHostedService<SMSM>();
        })
        .Build();

        await host.RunAsync();

        SMSM.Stop();
    }
}


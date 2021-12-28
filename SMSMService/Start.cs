using SMSMService;

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
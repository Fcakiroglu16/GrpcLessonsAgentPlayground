using LocationService.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<LocationStreamingWorker>();

var host = builder.Build();
host.Run();

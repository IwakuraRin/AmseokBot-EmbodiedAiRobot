using NasForWindows.Agent.Features.Operations;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options => options.ServiceName = "NasForWindows Agent");
builder.Services.AddHostedService<OperationWorker>();

var host = builder.Build();
host.Run();

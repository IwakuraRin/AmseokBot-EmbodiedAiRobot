using NasForWindows.Agent.Features.Operations;
using NasForWindows.Agent.Features.SystemOverview;
using NasForWindows.Agent.Infrastructure.Ipc;
using NasForWindows.Windows.Hardware;

if (!OperatingSystem.IsWindows())
{
    throw new PlatformNotSupportedException("NasForWindows Agent requires Windows.");
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options => options.ServiceName = "NasForWindows Agent");
builder.Services.AddSingleton<IHardwarePlatform, WindowsHardwarePlatform>();
builder.Services.AddSingleton<HardwareSnapshotStore>();
builder.Services.AddHostedService<OperationWorker>();
builder.Services.AddHostedService<HardwareMetricsWorker>();
builder.Services.AddHostedService<AgentIpcWorker>();

var host = builder.Build();
host.Run();

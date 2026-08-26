using NasForWindows.Windows.Hardware;

namespace NasForWindows.Agent.Features.SystemOverview;

internal sealed partial class HardwareMetricsWorker(
    IHardwarePlatform hardwarePlatform,
    HardwareSnapshotStore snapshots,
    ILogger<HardwareMetricsWorker> logger) : BackgroundService
{
    private static readonly TimeSpan MetricsInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan InventoryRefreshInterval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextInventoryRefresh = DateTimeOffset.MinValue;
        using var timer = new PeriodicTimer(MetricsInterval);

        do
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                if (snapshots.Inventory is null || now >= nextInventoryRefresh)
                {
                    snapshots.SetInventory(await hardwarePlatform.ReadInventoryAsync(stoppingToken));
                    nextInventoryRefresh = now + InventoryRefreshInterval;
                }

                snapshots.SetMetrics(await hardwarePlatform.SampleMetricsAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (
                exception is not OutOfMemoryException and not StackOverflowException)
            {
                LogCollectionFailed(logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Warning,
        Message = "Windows hardware collection failed; the last successful snapshot remains available")]
    private static partial void LogCollectionFailed(ILogger logger, Exception exception);
}

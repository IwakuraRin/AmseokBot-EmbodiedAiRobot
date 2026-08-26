using Microsoft.Extensions.Logging.Abstractions;
using NasForWindows.Agent.Features.SystemOverview;
using NasForWindows.Contracts.System;
using NasForWindows.Windows.Hardware;

namespace NasForWindows.Agent.Tests;

public sealed class HardwareMetricsWorkerTests
{
    [Fact]
    public async Task WorkerPublishesInventoryAndMetricsFromPlatformBoundary()
    {
        var inventory = CreateInventory();
        var metrics = CreateMetrics();
        var platform = new FakeHardwarePlatform(inventory, metrics);
        var snapshots = new HardwareSnapshotStore();
        var worker = new HardwareMetricsWorker(
            platform,
            snapshots,
            NullLogger<HardwareMetricsWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await platform.Sampled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Same(inventory, snapshots.Inventory);
        Assert.Same(metrics, snapshots.Metrics);

        await worker.StopAsync(CancellationToken.None);
    }

    private static HardwareInventoryResponse CreateInventory() => new(
        DateTimeOffset.UnixEpoch,
        "Windows",
        new CpuDeviceResponse("Test CPU", 4, 8),
        16UL * 1024 * 1024 * 1024,
        [new GpuDeviceResponse("gpu-1", "Test GPU", "Test", GpuMemoryKind.Dedicated, 8UL * 1024)],
        [new PhysicalDiskResponse("disk-1", "Test disk", null, 1_000_000, "NVMe")],
        new MainboardResponse("Test", "Board", "1"));

    private static HardwareMetricsResponse CreateMetrics() => new(
        DateTimeOffset.UnixEpoch,
        2,
        new CpuMetricsResponse(25, MetricAvailability.Available),
        new MemoryMetricsResponse(100, 40, 60, 40),
        [new GpuMetricsResponse(
            "gpu-1",
            50,
            4UL * 1024,
            MetricAvailability.Available,
            MetricAvailability.Available)]);

    private sealed class FakeHardwarePlatform(
        HardwareInventoryResponse inventory,
        HardwareMetricsResponse metrics) : IHardwarePlatform
    {
        internal TaskCompletionSource Sampled { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<HardwareInventoryResponse> ReadInventoryAsync(
            CancellationToken cancellationToken) => ValueTask.FromResult(inventory);

        public ValueTask<HardwareMetricsResponse> SampleMetricsAsync(
            CancellationToken cancellationToken)
        {
            Sampled.TrySetResult();
            return ValueTask.FromResult(metrics);
        }
    }
}

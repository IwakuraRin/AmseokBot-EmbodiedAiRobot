using NasForWindows.Contracts.System;

namespace NasForWindows.Agent.Features.SystemOverview;

internal sealed class HardwareSnapshotStore
{
    private HardwareInventoryResponse? inventory;
    private HardwareMetricsResponse? metrics;

    internal HardwareInventoryResponse? Inventory => Volatile.Read(ref inventory);

    internal HardwareMetricsResponse? Metrics => Volatile.Read(ref metrics);

    internal void SetInventory(HardwareInventoryResponse value) => Volatile.Write(ref inventory, value);

    internal void SetMetrics(HardwareMetricsResponse value) => Volatile.Write(ref metrics, value);
}

using NasForWindows.Contracts.System;

namespace NasForWindows.Windows.Hardware;

public interface IHardwarePlatform
{
    ValueTask<HardwareInventoryResponse> ReadInventoryAsync(CancellationToken cancellationToken);

    ValueTask<HardwareMetricsResponse> SampleMetricsAsync(CancellationToken cancellationToken);
}

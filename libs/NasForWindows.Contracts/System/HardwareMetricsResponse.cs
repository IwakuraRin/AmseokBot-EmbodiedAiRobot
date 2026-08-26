using System.Text.Json.Serialization;

namespace NasForWindows.Contracts.System;

public sealed record HardwareMetricsResponse(
    DateTimeOffset SampledAt,
    double SampleIntervalSeconds,
    CpuMetricsResponse Cpu,
    MemoryMetricsResponse Memory,
    IReadOnlyList<GpuMetricsResponse> Gpus);

public sealed record CpuMetricsResponse(
    double? UtilizationPercent,
    MetricAvailability Availability);

public sealed record MemoryMetricsResponse(
    ulong TotalBytes,
    ulong UsedBytes,
    ulong AvailableBytes,
    double UtilizationPercent);

public sealed record GpuMetricsResponse(
    string DeviceId,
    double? UtilizationPercent,
    ulong? MemoryUsedBytes,
    MetricAvailability UtilizationAvailability,
    MetricAvailability MemoryAvailability);

[JsonConverter(typeof(JsonStringEnumConverter<MetricAvailability>))]
public enum MetricAvailability
{
    Available,
    Unsupported,
    DriverUnavailable,
    PermissionDenied,
    TemporarilyUnavailable,
}

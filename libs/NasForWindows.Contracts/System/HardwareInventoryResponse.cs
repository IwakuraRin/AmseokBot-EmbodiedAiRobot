using System.Text.Json.Serialization;

namespace NasForWindows.Contracts.System;

public sealed record HardwareInventoryResponse(
    DateTimeOffset CollectedAt,
    string OperatingSystem,
    CpuDeviceResponse Cpu,
    ulong TotalMemoryBytes,
    IReadOnlyList<GpuDeviceResponse> Gpus,
    IReadOnlyList<PhysicalDiskResponse> PhysicalDisks,
    MainboardResponse Mainboard);

public sealed record CpuDeviceResponse(string Model, int PhysicalCoreCount, int LogicalProcessorCount);

public sealed record GpuDeviceResponse(
    string Id,
    string Model,
    string Vendor,
    GpuMemoryKind MemoryKind,
    ulong? DedicatedMemoryBytes);

public sealed record PhysicalDiskResponse(
    string Id,
    string Model,
    string? SerialNumber,
    ulong SizeBytes,
    string BusType);

public sealed record MainboardResponse(string? Manufacturer, string? Model, string? Version);

[JsonConverter(typeof(JsonStringEnumConverter<GpuMemoryKind>))]
public enum GpuMemoryKind
{
    Unknown,
    Dedicated,
    Shared,
}

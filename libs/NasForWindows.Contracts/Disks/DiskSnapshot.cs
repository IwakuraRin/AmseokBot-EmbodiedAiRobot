namespace NasForWindows.Contracts.Disks;

public sealed record DiskSnapshot(
    string Id,
    string FriendlyName,
    string MediaType,
    string BusType,
    long SizeBytes,
    string HealthStatus);

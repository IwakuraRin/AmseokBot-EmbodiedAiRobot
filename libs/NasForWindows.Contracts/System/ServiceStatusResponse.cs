namespace NasForWindows.Contracts.System;

public sealed record ServiceStatusResponse(string Service, string Status, DateTimeOffset Timestamp);

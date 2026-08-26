namespace NasForWindows.Operations;

public sealed record OperationSnapshot(
    OperationId Id,
    string Kind,
    OperationState State,
    int ProgressPercent,
    DateTimeOffset CreatedAt,
    string? ErrorCode);

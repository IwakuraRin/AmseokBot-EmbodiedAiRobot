namespace NasForWindows.Contracts.Agent;

public sealed record AgentResult<T>(Guid RequestId, bool Succeeded, T? Value, string? ErrorCode);

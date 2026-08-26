namespace NasForWindows.Contracts.Agent;

public sealed record AgentCommandRequest(Guid RequestId, AgentCommand Command);

public enum AgentCommand
{
    GetHardwareInventory = 1,
    GetHardwareMetrics = 2,
}

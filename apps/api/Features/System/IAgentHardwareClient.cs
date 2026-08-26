using NasForWindows.Contracts.System;

namespace NasForWindows.Api.Features.System;

internal interface IAgentHardwareClient
{
    Task<HardwareInventoryResponse> GetInventoryAsync(CancellationToken cancellationToken);

    Task<HardwareMetricsResponse> GetMetricsAsync(CancellationToken cancellationToken);
}

internal sealed class AgentHardwareUnavailableException(string errorCode, Exception? innerException = null)
    : Exception("The privileged Agent could not provide hardware information.", innerException)
{
    internal string ErrorCode { get; } = errorCode;
}

using NasForWindows.Api.Features.System.GetHardwareInventory;
using NasForWindows.Api.Features.System.GetHardwareMetrics;
using NasForWindows.Api.Features.System.GetStatus;
using NasForWindows.Api.Infrastructure.AgentIpc;

namespace NasForWindows.Api.Features.System;

internal static class SystemComposition
{
    internal static IServiceCollection AddSystemFeature(this IServiceCollection services)
    {
        services.AddSingleton<IAgentHardwareClient, NamedPipeAgentHardwareClient>();
        return services;
    }

    internal static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSystemStatus();
        endpoints.MapHardwareInventory();
        endpoints.MapHardwareMetrics();
        return endpoints;
    }
}

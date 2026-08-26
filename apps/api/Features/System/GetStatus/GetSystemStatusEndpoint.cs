using NasForWindows.Contracts.System;

namespace NasForWindows.Api.Features.System.GetStatus;

internal static class GetSystemStatusEndpoint
{
    internal static IEndpointRouteBuilder MapSystemStatus(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                "/api/system/status",
                () => new ServiceStatusResponse("NasForWindows.Api", "online", DateTimeOffset.UtcNow))
            .WithName("GetSystemStatus")
            .WithTags("System");

        return endpoints;
    }
}

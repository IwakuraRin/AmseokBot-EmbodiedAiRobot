using NasForWindows.Api.Features.WebAccess;
using NasForWindows.Contracts.System;

namespace NasForWindows.Api.Features.System.GetHardwareMetrics;

internal static class GetHardwareMetricsEndpoint
{
    internal static IEndpointRouteBuilder MapHardwareMetrics(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                "/api/system/metrics",
                async (IAgentHardwareClient client, CancellationToken cancellationToken) =>
                    await ExecuteAsync(client, cancellationToken))
            .WithName("GetHardwareMetrics")
            .WithTags("System")
            .Produces<HardwareMetricsResponse>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization(WebAccessSecurity.SystemOverviewRead);

        return endpoints;
    }

    private static async Task<IResult> ExecuteAsync(
        IAgentHardwareClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await client.GetMetricsAsync(cancellationToken));
        }
        catch (AgentHardwareUnavailableException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Privileged Agent unavailable",
                extensions: new Dictionary<string, object?> { ["code"] = exception.ErrorCode });
        }
    }
}

using NasForWindows.Api.Features.WebAccess;
using NasForWindows.Contracts.System;

namespace NasForWindows.Api.Features.System.GetHardwareInventory;

internal static class GetHardwareInventoryEndpoint
{
    internal static IEndpointRouteBuilder MapHardwareInventory(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                "/api/system/hardware",
                async (IAgentHardwareClient client, CancellationToken cancellationToken) =>
                    await ExecuteAsync(client, cancellationToken))
            .WithName("GetHardwareInventory")
            .WithTags("System")
            .Produces<HardwareInventoryResponse>()
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
            return Results.Ok(await client.GetInventoryAsync(cancellationToken));
        }
        catch (AgentHardwareUnavailableException exception)
        {
            return Unavailable(exception.ErrorCode);
        }
    }

    private static IResult Unavailable(string errorCode) => Results.Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable,
        title: "Privileged Agent unavailable",
        extensions: new Dictionary<string, object?> { ["code"] = errorCode });
}

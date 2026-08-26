using System.Net;
using NasForWindows.Api.Features.Audit;
using NasForWindows.Api.Features.WebAccess.Authentication;

namespace NasForWindows.Api.Features.WebAccess.Bootstrap;

internal static class BootstrapEndpoints
{
    internal static IEndpointRouteBuilder MapBootstrapEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/bootstrap/status",
                async (
                    HttpContext context,
                    BootstrapService bootstrapService,
                    CancellationToken cancellationToken) =>
                {
                    var status = await bootstrapService.GetStatusAsync(cancellationToken);
                    var isLocal = context.Connection.RemoteIpAddress is { } address
                        && IPAddress.IsLoopback(address);
                    return Results.Ok(new { status.RequiresBootstrap, CanInitialize = isLocal });
                })
            .AllowAnonymous()
            .WithName("GetBootstrapStatus")
            .WithTags("Bootstrap");

        endpoints.MapPost(
                "/api/bootstrap/token",
                async (
                    HttpContext context,
                    BootstrapService bootstrapService,
                    IAuditWriter auditWriter,
                    CancellationToken cancellationToken) =>
                {
                    var result = await bootstrapService.GenerateTokenAsync(cancellationToken);
                    await auditWriter.WriteAsync(
                        context,
                        new AuditEntry(
                            "bootstrap.token.generate",
                            result.Succeeded ? "succeeded" : "rejected"),
                        cancellationToken);
                    return result.Succeeded
                        ? Results.Ok(new { result.Token, result.ExpiresAtUtc })
                        : Results.Conflict(new { error = "Bootstrap is already complete." });
                })
            .AllowAnonymous()
            .AddEndpointFilter<LoopbackOnlyEndpointFilter>()
            .WithName("GenerateBootstrapToken")
            .WithTags("Bootstrap");

        endpoints.MapPost(
                "/api/bootstrap/owner",
                async (
                    BootstrapOwnerRequest request,
                    HttpContext context,
                    BootstrapService bootstrapService,
                    IAuditWriter auditWriter,
                    CancellationToken cancellationToken) =>
                {
                    var result = await bootstrapService.CreateOwnerAsync(request, cancellationToken);
                    await auditWriter.WriteAsync(
                        context,
                        new AuditEntry(
                            "bootstrap.owner.create",
                            result.Succeeded ? "succeeded" : "failed",
                            "web-user",
                            result.UserId,
                            ActorUserId: result.UserId,
                            ActorName: result.UserName),
                        cancellationToken);
                    return result.Succeeded
                        ? Results.Ok(new { result.UserId, result.UserName })
                        : Results.BadRequest(new { errors = result.Errors });
                })
            .AllowAnonymous()
            .AddEndpointFilter<LoopbackOnlyEndpointFilter>()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateBootstrapOwner")
            .WithTags("Bootstrap");

        return endpoints;
    }
}

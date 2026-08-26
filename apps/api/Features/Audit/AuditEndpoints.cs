using Microsoft.EntityFrameworkCore;
using NasForWindows.Api.Features.WebAccess;

namespace NasForWindows.Api.Features.Audit;

internal static class AuditEndpoints
{
    internal static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/audit",
                async (int? take, AuditDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    var resultLimit = Math.Clamp(take ?? 100, 1, 200);
                    var events = await dbContext.AuditEvents
                        .AsNoTracking()
                        .OrderByDescending(auditEvent => auditEvent.Id)
                        .Take(resultLimit)
                        .Select(auditEvent => new AuditEventResponse(
                            auditEvent.Id,
                            auditEvent.OccurredAtUtc,
                            auditEvent.ActorUserId,
                            auditEvent.ActorName,
                            auditEvent.Action,
                            auditEvent.TargetType,
                            auditEvent.TargetId,
                            auditEvent.Outcome,
                            auditEvent.SourceIp,
                            auditEvent.CorrelationId,
                            auditEvent.Details))
                        .ToArrayAsync(cancellationToken);

                    return Results.Ok(events);
                })
            .RequireAuthorization(WebAccessSecurity.AuditRead)
            .WithName("GetAuditEvents")
            .WithTags("Audit");

        return endpoints;
    }

    private sealed record AuditEventResponse(
        long Id,
        DateTimeOffset OccurredAtUtc,
        string? ActorUserId,
        string? ActorName,
        string Action,
        string? TargetType,
        string? TargetId,
        string Outcome,
        string? SourceIp,
        string CorrelationId,
        string? Details);
}

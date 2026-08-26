using System.Security.Claims;

namespace NasForWindows.Api.Features.Audit;

internal sealed class AuditWriter(AuditDbContext dbContext) : IAuditWriter
{
    public async Task WriteAsync(
        HttpContext context,
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = new AuditEvent
        {
            OccurredAtUtc = DateTimeOffset.UtcNow,
            ActorUserId = entry.ActorUserId ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            ActorName = entry.ActorName ?? context.User.Identity?.Name,
            Action = entry.Action,
            TargetType = entry.TargetType,
            TargetId = entry.TargetId,
            Outcome = entry.Outcome,
            SourceIp = context.Connection.RemoteIpAddress?.ToString(),
            CorrelationId = context.TraceIdentifier,
            Details = entry.Details,
        };

        dbContext.AuditEvents.Add(auditEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

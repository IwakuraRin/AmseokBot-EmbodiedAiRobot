namespace NasForWindows.Api.Features.Audit;

internal sealed record AuditEntry(
    string Action,
    string Outcome,
    string? TargetType = null,
    string? TargetId = null,
    string? Details = null,
    string? ActorUserId = null,
    string? ActorName = null);

internal interface IAuditWriter
{
    Task WriteAsync(HttpContext context, AuditEntry entry, CancellationToken cancellationToken = default);
}

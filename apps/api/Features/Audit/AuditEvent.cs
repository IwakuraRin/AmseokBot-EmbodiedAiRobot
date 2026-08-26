namespace NasForWindows.Api.Features.Audit;

internal sealed class AuditEvent
{
    public long Id { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string? ActorUserId { get; set; }

    public string? ActorName { get; set; }

    public required string Action { get; set; }

    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    public required string Outcome { get; set; }

    public string? SourceIp { get; set; }

    public required string CorrelationId { get; set; }

    public string? Details { get; set; }
}

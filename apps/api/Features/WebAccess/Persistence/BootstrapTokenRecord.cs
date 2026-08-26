namespace NasForWindows.Api.Features.WebAccess.Persistence;

internal sealed class BootstrapTokenRecord
{
    public long Id { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? ConsumedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

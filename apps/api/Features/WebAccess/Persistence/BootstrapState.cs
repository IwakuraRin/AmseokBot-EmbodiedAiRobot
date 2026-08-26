namespace NasForWindows.Api.Features.WebAccess.Persistence;

internal sealed class BootstrapState
{
    public int Id { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }
}

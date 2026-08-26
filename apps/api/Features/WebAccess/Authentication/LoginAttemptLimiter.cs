using System.Threading.RateLimiting;

namespace NasForWindows.Api.Features.WebAccess.Authentication;

internal sealed class LoginAttemptLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<string> _ipLimiter =
        PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));

    private readonly PartitionedRateLimiter<string> _userNameLimiter =
        PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));

    internal async ValueTask<bool> AcquireAsync(
        string ipAddress,
        string normalizedUserName,
        CancellationToken cancellationToken)
    {
        using var ipLease = await _ipLimiter.AcquireAsync(ipAddress, 1, cancellationToken);
        using var userNameLease = await _userNameLimiter.AcquireAsync(
            normalizedUserName,
            1,
            cancellationToken);

        return ipLease.IsAcquired && userNameLease.IsAcquired;
    }

    public void Dispose()
    {
        _ipLimiter.Dispose();
        _userNameLimiter.Dispose();
    }
}

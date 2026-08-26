using System.Security.Claims;
using NasForWindows.Api.Features.WebAccess.Persistence;

namespace NasForWindows.Api.Features.WebAccess.Authorization;

internal sealed record CurrentWebAccess(
    WebUser User,
    IReadOnlyList<string> Roles,
    IReadOnlySet<string> Permissions);

internal interface ICurrentWebAccessResolver
{
    Task<CurrentWebAccess?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}

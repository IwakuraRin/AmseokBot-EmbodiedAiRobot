using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NasForWindows.Api.Features.WebAccess.Persistence;

namespace NasForWindows.Api.Features.WebAccess.Authorization;

internal sealed class CurrentWebAccessResolver(UserManager<WebUser> userManager)
    : ICurrentWebAccessResolver
{
    public async Task<CurrentWebAccess?> ResolveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsEnabled)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var roles = (await userManager.GetRolesAsync(user)).Order(StringComparer.Ordinal).ToArray();
        var permissions = WebAccessRoles.ResolvePermissions(roles);
        return new CurrentWebAccess(user, roles, permissions);
    }
}

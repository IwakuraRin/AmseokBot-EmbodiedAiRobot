using Microsoft.AspNetCore.Authorization;

namespace NasForWindows.Api.Features.WebAccess.Authorization;

internal sealed class PermissionAuthorizationHandler(
    ICurrentWebAccessResolver accessResolver,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!WebAccessPermissions.All.Contains(requirement.Permission))
        {
            return;
        }

        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var access = await accessResolver.ResolveAsync(context.User, cancellationToken);
        if (access?.Permissions.Contains(requirement.Permission) is true)
        {
            context.Succeed(requirement);
        }
    }
}

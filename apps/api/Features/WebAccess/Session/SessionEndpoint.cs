using NasForWindows.Api.Features.WebAccess.Authorization;

namespace NasForWindows.Api.Features.WebAccess.Session;

internal static class SessionEndpoint
{
    internal static IEndpointRouteBuilder MapSessionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/session",
                async (
                    HttpContext context,
                    ICurrentWebAccessResolver accessResolver,
                    CancellationToken cancellationToken) =>
                {
                    var access = await accessResolver.ResolveAsync(context.User, cancellationToken);
                    if (access is null)
                    {
                        return Results.Unauthorized();
                    }

                    return Results.Ok(new SessionResponse(
                        new SessionUserResponse(
                            access.User.Id,
                            access.User.UserName ?? string.Empty,
                            access.User.DisplayName),
                        access.Roles,
                        access.Permissions.Order(StringComparer.Ordinal).ToArray()));
                })
            .RequireAuthorization()
            .WithName("GetSession")
            .WithTags("Session");

        return endpoints;
    }

    private sealed record SessionResponse(
        SessionUserResponse User,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions);

    private sealed record SessionUserResponse(string Id, string UserName, string DisplayName);
}

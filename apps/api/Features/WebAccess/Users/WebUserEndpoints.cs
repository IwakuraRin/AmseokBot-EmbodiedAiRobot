using NasForWindows.Api.Features.Audit;
using NasForWindows.Api.Features.WebAccess.Authentication;
using NasForWindows.Api.Features.WebAccess.Authorization;

namespace NasForWindows.Api.Features.WebAccess.Users;

internal static class WebUserEndpoints
{
    internal static IEndpointRouteBuilder MapWebUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/web-users")
            .RequireAuthorization(WebAccessPermissions.WebUsersManage)
            .WithTags("Web users");

        group.MapGet(
                "/",
                async (WebUserAdministration administration, CancellationToken cancellationToken) =>
                    Results.Ok(await administration.ListAsync(cancellationToken)))
            .WithName("ListWebUsers");

        group.MapPost(
                "/",
                async (
                    CreateWebUserRequest request,
                    HttpContext context,
                    WebUserAdministration administration,
                    IAuditWriter auditWriter,
                    CancellationToken cancellationToken) =>
                {
                    var result = await administration.CreateAsync(request, cancellationToken);
                    await AuditMutationAsync(
                        context,
                        auditWriter,
                        "web-user.create",
                        result,
                        cancellationToken);
                    return ToHttpResult(result, created: true);
                })
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("CreateWebUser");

        group.MapPut(
                "/{userId}",
                async (
                    string userId,
                    UpdateWebUserRequest request,
                    HttpContext context,
                    WebUserAdministration administration,
                    IAuditWriter auditWriter,
                    CancellationToken cancellationToken) =>
                {
                    var result = await administration.UpdateAsync(userId, request, cancellationToken);
                    await AuditMutationAsync(
                        context,
                        auditWriter,
                        "web-user.update",
                        result with { UserId = result.UserId ?? userId },
                        cancellationToken);
                    return ToHttpResult(result);
                })
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("UpdateWebUser");

        group.MapDelete(
                "/{userId}",
                async (
                    string userId,
                    HttpContext context,
                    WebUserAdministration administration,
                    IAuditWriter auditWriter,
                    CancellationToken cancellationToken) =>
                {
                    var result = await administration.DeleteAsync(userId, cancellationToken);
                    await AuditMutationAsync(
                        context,
                        auditWriter,
                        "web-user.delete",
                        result with { UserId = result.UserId ?? userId },
                        cancellationToken);
                    return ToHttpResult(result);
                })
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("DeleteWebUser");

        group.MapPut(
                "/{userId}/password",
                async (
                    string userId,
                    ResetWebUserPasswordRequest request,
                    HttpContext context,
                    WebUserAdministration administration,
                    IAuditWriter auditWriter,
                    CancellationToken cancellationToken) =>
                {
                    var result = await administration.ResetPasswordAsync(
                        userId,
                        request.Password,
                        cancellationToken);
                    await AuditMutationAsync(
                        context,
                        auditWriter,
                        "web-user.password.reset",
                        result with { UserId = result.UserId ?? userId },
                        cancellationToken);
                    return ToHttpResult(result);
                })
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("ResetWebUserPassword");

        return endpoints;
    }

    private static IResult ToHttpResult(WebUserMutationResult result, bool created = false)
    {
        if (!result.WasFound)
        {
            return Results.NotFound();
        }

        if (!result.WasSuccessful)
        {
            return Results.BadRequest(new { errors = result.Errors });
        }

        return created
            ? Results.Created($"/api/web-users/{result.UserId}", new { result.UserId })
            : Results.NoContent();
    }

    private static Task AuditMutationAsync(
        HttpContext context,
        IAuditWriter auditWriter,
        string action,
        WebUserMutationResult result,
        CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(
            context,
            new AuditEntry(
                action,
                result.WasSuccessful ? "succeeded" : "failed",
                "web-user",
                result.UserId),
            cancellationToken);
}

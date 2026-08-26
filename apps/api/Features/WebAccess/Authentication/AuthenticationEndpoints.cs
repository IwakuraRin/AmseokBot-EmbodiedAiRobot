using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using NasForWindows.Api.Features.Audit;
using NasForWindows.Api.Features.WebAccess.Persistence;

namespace NasForWindows.Api.Features.WebAccess.Authentication;

internal static class AuthenticationEndpoints
{
    internal static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/security/antiforgery",
                (HttpContext context, IAntiforgery antiforgery, IHostEnvironment environment) =>
                {
                    var tokens = antiforgery.GetAndStoreTokens(context);
                    context.Response.Cookies.Append(
                        "XSRF-TOKEN",
                        tokens.RequestToken!,
                        new CookieOptions
                        {
                            HttpOnly = false,
                            SameSite = SameSiteMode.Strict,
                            Secure = !environment.IsDevelopment() || context.Request.IsHttps,
                            Path = "/",
                        });
                    return Results.NoContent();
                })
            .AllowAnonymous()
            .WithName("IssueAntiforgeryToken")
            .WithTags("Authentication");

        endpoints.MapPost(
                "/api/auth/login",
                LoginAsync)
            .AllowAnonymous()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("Login")
            .WithTags("Authentication");

        endpoints.MapPost(
                "/api/auth/logout",
                async (
                    HttpContext context,
                    SignInManager<WebUser> signInManager,
                    IAuditWriter auditWriter,
                    CancellationToken cancellationToken) =>
                {
                    await auditWriter.WriteAsync(
                        context,
                        new AuditEntry("authentication.logout", "succeeded"),
                        cancellationToken);
                    await signInManager.SignOutAsync();
                    return Results.NoContent();
                })
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryEndpointFilter>()
            .WithName("Logout")
            .WithTags("Authentication");

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext context,
        UserManager<WebUser> userManager,
        SignInManager<WebUser> signInManager,
        ILookupNormalizer normalizer,
        LoginAttemptLimiter attemptLimiter,
        IAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        var normalizedUserName = normalizer.NormalizeName(request.UserName?.Trim() ?? string.Empty)
            ?? string.Empty;
        var sourceIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (!await attemptLimiter.AcquireAsync(sourceIp, normalizedUserName, cancellationToken))
        {
            await auditWriter.WriteAsync(
                context,
                new AuditEntry("authentication.login", "rate-limited"),
                cancellationToken);
            return Results.Json(
                new { error = "Login failed." },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        var user = await userManager.FindByNameAsync(request.UserName?.Trim() ?? string.Empty);
        if (user is null || !user.IsEnabled)
        {
            await auditWriter.WriteAsync(
                context,
                new AuditEntry("authentication.login", "failed"),
                cancellationToken);
            return Results.Unauthorized();
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password ?? string.Empty,
            request.RememberMe,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            await auditWriter.WriteAsync(
                context,
                new AuditEntry(
                    "authentication.login",
                    "failed",
                    "web-user",
                    user.Id,
                    ActorUserId: user.Id,
                    ActorName: user.UserName),
                cancellationToken);
            return Results.Unauthorized();
        }

        await auditWriter.WriteAsync(
            context,
            new AuditEntry(
                "authentication.login",
                "succeeded",
                "web-user",
                user.Id,
                ActorUserId: user.Id,
                ActorName: user.UserName),
            cancellationToken);
        return Results.NoContent();
    }

    private sealed record LoginRequest(string? UserName, string? Password, bool RememberMe);
}

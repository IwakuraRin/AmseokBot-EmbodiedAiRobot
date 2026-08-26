using Microsoft.AspNetCore.Authorization;

namespace NasForWindows.Api.Features.Audit;

internal sealed class AuthorizationAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuditWriter auditWriter)
    {
        await next(context);

        var allowsAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        if (!allowsAnonymous
            && context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            var outcome = context.Response.StatusCode == StatusCodes.Status401Unauthorized
                ? "unauthenticated"
                : "denied";

            await auditWriter.WriteAsync(
                context,
                new AuditEntry(
                    "authorization.evaluate",
                    outcome,
                    "http-endpoint",
                    $"{context.Request.Method} {context.Request.Path}"),
                CancellationToken.None);
        }
    }
}

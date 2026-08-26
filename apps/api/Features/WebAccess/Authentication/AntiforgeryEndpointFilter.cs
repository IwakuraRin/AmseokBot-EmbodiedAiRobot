using Microsoft.AspNetCore.Antiforgery;

namespace NasForWindows.Api.Features.WebAccess.Authentication;

internal sealed class AntiforgeryEndpointFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest(new { error = "The antiforgery token is invalid or missing." });
        }

        return await next(context);
    }
}

using System.Net;

namespace NasForWindows.Api.Features.WebAccess.Bootstrap;

internal sealed class LoopbackOnlyEndpointFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var remoteAddress = context.HttpContext.Connection.RemoteIpAddress;
        if (remoteAddress is null || !IPAddress.IsLoopback(remoteAddress))
        {
            return ValueTask.FromResult<object?>(Results.NotFound());
        }

        return next(context);
    }
}

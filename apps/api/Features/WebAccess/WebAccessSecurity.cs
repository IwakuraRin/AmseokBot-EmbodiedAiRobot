using NasForWindows.Api.Features.WebAccess.Authentication;
using NasForWindows.Api.Features.WebAccess.Authorization;
using NasForWindows.Api.Features.WebAccess.Bootstrap;
using NasForWindows.Api.Features.WebAccess.Session;
using NasForWindows.Api.Features.WebAccess.Users;

namespace NasForWindows.Api.Features.WebAccess;

internal static class WebAccessSecurity
{
    internal const string SystemOverviewRead = WebAccessPermissions.SystemOverviewRead;
    internal const string AuditRead = WebAccessPermissions.AuditRead;
    internal const string StorageRead = WebAccessPermissions.StorageRead;
    internal const string StorageManage = WebAccessPermissions.StorageManage;
    internal const string StorageDestroy = WebAccessPermissions.StorageDestroy;
    internal const string SharesRead = WebAccessPermissions.SharesRead;
    internal const string OperationsRead = WebAccessPermissions.OperationsRead;
    internal const string WebUsersManage = WebAccessPermissions.WebUsersManage;

    internal const string OwnerRole = WebAccessRoles.Owner;
    internal const string OperatorRole = WebAccessRoles.Operator;
    internal const string ViewerRole = WebAccessRoles.Viewer;

    internal static IReadOnlySet<string> AllPermissions => WebAccessPermissions.All;

    internal static IReadOnlySet<string> ResolvePermissions(IEnumerable<string> roles) =>
        WebAccessRoles.ResolvePermissions(roles);

    internal static IEndpointRouteBuilder MapWebAccessSecurityEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAuthenticationEndpoints();
        endpoints.MapBootstrapEndpoints();
        endpoints.MapSessionEndpoint();
        endpoints.MapWebUserEndpoints();
        return endpoints;
    }
}

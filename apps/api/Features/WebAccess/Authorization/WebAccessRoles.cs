namespace NasForWindows.Api.Features.WebAccess.Authorization;

internal static class WebAccessRoles
{
    internal const string Owner = "Owner";
    internal const string Operator = "Operator";
    internal const string Viewer = "Viewer";

    internal static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Owner,
        Operator,
        Viewer,
    };

    private static readonly Dictionary<string, IReadOnlySet<string>> PermissionsByRole =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [Owner] = WebAccessPermissions.All,
            [Operator] = new HashSet<string>(StringComparer.Ordinal)
            {
                WebAccessPermissions.SystemOverviewRead,
                WebAccessPermissions.StorageRead,
                WebAccessPermissions.StorageManage,
                WebAccessPermissions.SharesRead,
                WebAccessPermissions.SharesManage,
                WebAccessPermissions.OperationsRead,
                WebAccessPermissions.OperationsCancel,
            },
            [Viewer] = new HashSet<string>(StringComparer.Ordinal)
            {
                WebAccessPermissions.SystemOverviewRead,
                WebAccessPermissions.StorageRead,
                WebAccessPermissions.SharesRead,
                WebAccessPermissions.OperationsRead,
            },
        };

    internal static IReadOnlySet<string> ResolvePermissions(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in roles)
        {
            if (PermissionsByRole.TryGetValue(role, out var rolePermissions))
            {
                permissions.UnionWith(rolePermissions);
            }
        }

        return permissions;
    }
}

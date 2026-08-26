namespace NasForWindows.Api.Features.WebAccess.Authorization;

internal static class WebAccessPermissions
{
    internal const string SystemOverviewRead = "system.overview.read";
    internal const string StorageRead = "storage.read";
    internal const string StorageManage = "storage.manage";
    internal const string StorageDestroy = "storage.destroy";
    internal const string SharesRead = "shares.read";
    internal const string SharesManage = "shares.manage";
    internal const string OperationsRead = "operations.read";
    internal const string OperationsCancel = "operations.cancel";
    internal const string WebUsersManage = "web.users.manage";
    internal const string PluginsManage = "plugins.manage";
    internal const string SettingsManage = "settings.manage";
    internal const string AuditRead = "audit.read";

    internal static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        SystemOverviewRead,
        StorageRead,
        StorageManage,
        StorageDestroy,
        SharesRead,
        SharesManage,
        OperationsRead,
        OperationsCancel,
        WebUsersManage,
        PluginsManage,
        SettingsManage,
        AuditRead,
    };
}

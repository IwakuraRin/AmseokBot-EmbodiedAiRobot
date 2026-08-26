using NasForWindows.Api.Features.WebAccess;

namespace NasForWindows.Api.Tests;

public sealed class WebAccessRoleTests
{
    [Fact]
    public void OwnerReceivesEveryCorePermission()
    {
        var permissions = WebAccessSecurity.ResolvePermissions([WebAccessSecurity.OwnerRole]);

        Assert.Equal(WebAccessSecurity.AllPermissions.Order(), permissions.Order());
    }

    [Fact]
    public void OperatorCannotDestroyStorageOrManageWebUsers()
    {
        var permissions = WebAccessSecurity.ResolvePermissions([WebAccessSecurity.OperatorRole]);

        Assert.Contains(WebAccessSecurity.StorageManage, permissions);
        Assert.DoesNotContain(WebAccessSecurity.StorageDestroy, permissions);
        Assert.DoesNotContain(WebAccessSecurity.WebUsersManage, permissions);
    }

    [Fact]
    public void ViewerReceivesReadOnlyPermissions()
    {
        var permissions = WebAccessSecurity.ResolvePermissions([WebAccessSecurity.ViewerRole]);

        Assert.Equal(
            [
                WebAccessSecurity.OperationsRead,
                WebAccessSecurity.SharesRead,
                WebAccessSecurity.StorageRead,
                WebAccessSecurity.SystemOverviewRead,
            ],
            permissions.Order());
    }

    [Fact]
    public void UnknownRoleReceivesNoPermissions()
    {
        Assert.Empty(WebAccessSecurity.ResolvePermissions(["Administrator"]));
    }
}

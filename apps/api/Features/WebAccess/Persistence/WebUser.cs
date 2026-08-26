using Microsoft.AspNetCore.Identity;

namespace NasForWindows.Api.Features.WebAccess.Persistence;

internal sealed class WebUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
}

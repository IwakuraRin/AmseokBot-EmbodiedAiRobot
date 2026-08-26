using Microsoft.AspNetCore.Identity;
using NasForWindows.Api.Features.WebAccess.Authorization;

namespace NasForWindows.Api.Features.WebAccess.Bootstrap;

internal sealed class WebAccessRoleSeeder(RoleManager<IdentityRole> roleManager)
{
    internal async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var roleName in WebAccessRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Code));
                throw new InvalidOperationException($"Unable to seed role {roleName}: {errors}");
            }
        }
    }
}

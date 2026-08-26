using Microsoft.AspNetCore.Authorization;

namespace NasForWindows.Api.Features.WebAccess.Authorization;

internal sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NasForWindows.Api.Features.WebAccess.Authorization;
using NasForWindows.Api.Features.WebAccess.Persistence;

namespace NasForWindows.Api.Features.WebAccess.Users;

internal sealed class WebUserAdministration(
    WebAccessDbContext dbContext,
    UserManager<WebUser> userManager)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    internal async Task<IReadOnlyList<WebUserSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.AsNoTracking()
            .OrderBy(user => user.UserName)
            .ToArrayAsync(cancellationToken);
        var result = new List<WebUserSummary>(users.Length);

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new WebUserSummary(
                user.Id,
                user.UserName ?? string.Empty,
                user.DisplayName,
                user.IsEnabled,
                roles.SingleOrDefault() ?? string.Empty));
        }

        return result;
    }

    internal async Task<WebUserMutationResult> CreateAsync(
        CreateWebUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsKnownRole(request.Role))
        {
            return WebUserMutationResult.Failed("The role is invalid.");
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var user = new WebUser
            {
                UserName = request.UserName?.Trim(),
                DisplayName = request.DisplayName?.Trim() ?? string.Empty,
                IsEnabled = true,
            };
            if (string.IsNullOrWhiteSpace(user.DisplayName))
            {
                user.DisplayName = user.UserName ?? string.Empty;
            }

            var createResult = await userManager.CreateAsync(user, request.Password ?? string.Empty);
            if (!createResult.Succeeded)
            {
                return WebUserMutationResult.Failed(createResult.Errors.Select(error => error.Description));
            }

            var roleResult = await userManager.AddToRoleAsync(user, request.Role!);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return WebUserMutationResult.Failed(roleResult.Errors.Select(error => error.Description));
            }

            await transaction.CommitAsync(cancellationToken);
            return WebUserMutationResult.Succeeded(user.Id);
        }
        finally
        {
            Gate.Release();
        }
    }

    internal async Task<WebUserMutationResult> UpdateAsync(
        string userId,
        UpdateWebUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsKnownRole(request.Role))
        {
            return WebUserMutationResult.Failed("The role is invalid.");
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return WebUserMutationResult.NotFound();
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            var isOwner = currentRoles.Contains(WebAccessRoles.Owner, StringComparer.Ordinal);
            var removesEnabledOwner = isOwner
                && (!request.IsEnabled || !string.Equals(request.Role, WebAccessRoles.Owner, StringComparison.Ordinal));
            if (removesEnabledOwner && user.IsEnabled && await CountEnabledOwnersAsync(cancellationToken) <= 1)
            {
                return WebUserMutationResult.Failed("The last enabled Owner cannot be disabled or demoted.");
            }

            user.DisplayName = request.DisplayName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(user.DisplayName))
            {
                user.DisplayName = user.UserName ?? string.Empty;
            }
            user.IsEnabled = request.IsEnabled;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return WebUserMutationResult.Failed(updateResult.Errors.Select(error => error.Description));
            }

            if (!currentRoles.SequenceEqual([request.Role!], StringComparer.Ordinal))
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return WebUserMutationResult.Failed(removeResult.Errors.Select(error => error.Description));
                }

                var addResult = await userManager.AddToRoleAsync(user, request.Role!);
                if (!addResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return WebUserMutationResult.Failed(addResult.Errors.Select(error => error.Description));
                }
            }

            await userManager.UpdateSecurityStampAsync(user);
            await transaction.CommitAsync(cancellationToken);
            return WebUserMutationResult.Succeeded(user.Id);
        }
        finally
        {
            Gate.Release();
        }
    }

    internal async Task<WebUserMutationResult> DeleteAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return WebUserMutationResult.NotFound();
            }

            var roles = await userManager.GetRolesAsync(user);
            if (user.IsEnabled
                && roles.Contains(WebAccessRoles.Owner, StringComparer.Ordinal)
                && await CountEnabledOwnersAsync(cancellationToken) <= 1)
            {
                return WebUserMutationResult.Failed("The last enabled Owner cannot be deleted.");
            }

            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return WebUserMutationResult.Failed(deleteResult.Errors.Select(error => error.Description));
            }

            await transaction.CommitAsync(cancellationToken);
            return WebUserMutationResult.Succeeded(userId);
        }
        finally
        {
            Gate.Release();
        }
    }

    internal async Task<WebUserMutationResult> ResetPasswordAsync(
        string userId,
        string? password,
        CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return WebUserMutationResult.NotFound();
            }

            cancellationToken.ThrowIfCancellationRequested();
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, password ?? string.Empty);
            if (!result.Succeeded)
            {
                return WebUserMutationResult.Failed(result.Errors.Select(error => error.Description));
            }

            await userManager.UpdateSecurityStampAsync(user);
            return WebUserMutationResult.Succeeded(user.Id);
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<int> CountEnabledOwnersAsync(CancellationToken cancellationToken)
    {
        var normalizedOwner = userManager.NormalizeName(WebAccessRoles.Owner);
        return await (
            from user in dbContext.Users
            join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where user.IsEnabled && role.NormalizedName == normalizedOwner
            select user.Id).CountAsync(cancellationToken);
    }

    private static bool IsKnownRole(string? role) =>
        role is not null && WebAccessRoles.All.Contains(role);
}

internal sealed record WebUserSummary(
    string Id,
    string UserName,
    string DisplayName,
    bool IsEnabled,
    string Role);

internal sealed record CreateWebUserRequest(
    string? UserName,
    string? DisplayName,
    string? Password,
    string? Role);

internal sealed record UpdateWebUserRequest(string? DisplayName, bool IsEnabled, string? Role);

internal sealed record ResetWebUserPasswordRequest(string? Password);

internal sealed record WebUserMutationResult(
    bool WasSuccessful,
    bool WasFound,
    string? UserId,
    IReadOnlyList<string> Errors)
{
    internal static WebUserMutationResult Succeeded(string userId) => new(true, true, userId, []);

    internal static WebUserMutationResult Failed(string error) => new(false, true, null, [error]);

    internal static WebUserMutationResult Failed(IEnumerable<string> errors) =>
        new(false, true, null, errors.ToArray());

    internal static WebUserMutationResult NotFound() => new(false, false, null, []);
}

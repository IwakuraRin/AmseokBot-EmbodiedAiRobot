using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using NasForWindows.Api.Features.WebAccess.Authorization;
using NasForWindows.Api.Features.WebAccess.Persistence;

namespace NasForWindows.Api.Features.WebAccess.Bootstrap;

internal sealed class BootstrapService(
    WebAccessDbContext dbContext,
    UserManager<WebUser> userManager)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    internal async Task<BootstrapStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var state = await dbContext.BootstrapStates
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == 1, cancellationToken);
        return new BootstrapStatus(state?.CompletedAtUtc is null);
    }

    internal async Task<BootstrapTokenResult> GenerateTokenAsync(CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var state = await GetOrCreateStateAsync(cancellationToken);
            if (state.CompletedAtUtc is not null || await AnyOwnerExistsAsync(cancellationToken))
            {
                return BootstrapTokenResult.Unavailable();
            }

            var now = DateTimeOffset.UtcNow;
            var activeTokens = await dbContext.BootstrapTokens
                .Where(token => token.ConsumedAtUtc == null)
                .ToArrayAsync(cancellationToken);
            foreach (var activeToken in activeTokens)
            {
                activeToken.ConsumedAtUtc = now;
            }

            var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            dbContext.BootstrapTokens.Add(new BootstrapTokenRecord
            {
                TokenHash = HashToken(rawToken),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(10),
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return BootstrapTokenResult.Created(rawToken, now.AddMinutes(10));
        }
        finally
        {
            Gate.Release();
        }
    }

    internal async Task<BootstrapOwnerResult> CreateOwnerAsync(
        BootstrapOwnerRequest request,
        CancellationToken cancellationToken)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var state = await GetOrCreateStateAsync(cancellationToken);
            if (state.CompletedAtUtc is not null || await AnyOwnerExistsAsync(cancellationToken))
            {
                return BootstrapOwnerResult.Failed("Bootstrap is already complete.");
            }

            var tokenHash = HashToken(request.Token?.Trim() ?? string.Empty);
            var now = DateTimeOffset.UtcNow;
            var token = await dbContext.BootstrapTokens.SingleOrDefaultAsync(
                entry => entry.TokenHash == tokenHash,
                cancellationToken);
            if (token is null || token.ConsumedAtUtc is not null || token.ExpiresAtUtc <= now)
            {
                return BootstrapOwnerResult.Failed("The bootstrap token is invalid or expired.");
            }

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
                return BootstrapOwnerResult.Failed(createResult.Errors.Select(error => error.Description));
            }

            var roleResult = await userManager.AddToRoleAsync(user, WebAccessRoles.Owner);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return BootstrapOwnerResult.Failed(roleResult.Errors.Select(error => error.Description));
            }

            token.ConsumedAtUtc = now;
            state.CompletedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return BootstrapOwnerResult.Created(user.Id, user.UserName ?? string.Empty);
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<BootstrapState> GetOrCreateStateAsync(CancellationToken cancellationToken)
    {
        var state = await dbContext.BootstrapStates.SingleOrDefaultAsync(
            entry => entry.Id == 1,
            cancellationToken);
        if (state is not null)
        {
            return state;
        }

        state = new BootstrapState { Id = 1 };
        dbContext.BootstrapStates.Add(state);
        return state;
    }

    private async Task<bool> AnyOwnerExistsAsync(CancellationToken cancellationToken)
    {
        var normalizedOwner = userManager.NormalizeName(WebAccessRoles.Owner);
        return await (
            from user in dbContext.Users
            join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where role.NormalizedName == normalizedOwner
            select user.Id).AnyAsync(cancellationToken);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

internal sealed record BootstrapStatus(bool RequiresBootstrap);

internal sealed record BootstrapOwnerRequest(
    string? Token,
    string? UserName,
    string? DisplayName,
    string? Password);

internal sealed record BootstrapTokenResult(bool Succeeded, string? Token, DateTimeOffset? ExpiresAtUtc)
{
    internal static BootstrapTokenResult Created(string token, DateTimeOffset expiresAtUtc) =>
        new(true, token, expiresAtUtc);

    internal static BootstrapTokenResult Unavailable() => new(false, null, null);
}

internal sealed record BootstrapOwnerResult(
    bool Succeeded,
    string? UserId,
    string? UserName,
    IReadOnlyList<string> Errors)
{
    internal static BootstrapOwnerResult Created(string userId, string userName) =>
        new(true, userId, userName, []);

    internal static BootstrapOwnerResult Failed(string error) =>
        new(false, null, null, [error]);

    internal static BootstrapOwnerResult Failed(IEnumerable<string> errors) =>
        new(false, null, null, errors.ToArray());
}

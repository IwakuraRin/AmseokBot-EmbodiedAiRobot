using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NasForWindows.Api.Features.Audit;
using NasForWindows.Api.Features.WebAccess.Authentication;
using NasForWindows.Api.Features.WebAccess.Authorization;
using NasForWindows.Api.Features.WebAccess.Bootstrap;
using NasForWindows.Api.Features.WebAccess.Persistence;
using NasForWindows.Api.Features.WebAccess.Users;

namespace NasForWindows.Api.Features.WebAccess;

internal static class WebAccessComposition
{
    internal static IServiceCollection AddWebAccessSecurity(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        var configuredDataDirectory = configuration["Storage:DataDirectory"];
        var dataDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
            ? Path.Combine(environment.ContentRootPath, "data")
            : Path.GetFullPath(configuredDataDirectory, environment.ContentRootPath);
        var keyDirectory = Path.Combine(dataDirectory, "data-protection-keys");
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(keyDirectory);

        services.AddDbContext<WebAccessDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "web-access.db")}"));
        services.AddDbContext<AuditDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "audit.db")}"));

        var dataProtection = services.AddDataProtection()
            .SetApplicationName("NasForWindows.Api")
            .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
        if (OperatingSystem.IsWindows())
        {
            dataProtection.ProtectKeysWithDpapi();
        }

        services.AddIdentity<WebUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<WebAccessDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = environment.IsDevelopment()
                ? "NasForWindows.Auth"
                : "__Host-NasForWindows.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
        services.Configure<SecurityStampValidatorOptions>(options =>
            options.ValidationInterval = TimeSpan.FromMinutes(5));

        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = environment.IsDevelopment()
                ? "NasForWindows.Antiforgery"
                : "__Host-NasForWindows.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.Cookie.Path = "/";
            options.HeaderName = "X-XSRF-TOKEN";
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            foreach (var permission in WebAccessPermissions.All)
            {
                options.AddPolicy(
                    permission,
                    policy => policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentWebAccessResolver, CurrentWebAccessResolver>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<BootstrapService>();
        services.AddScoped<WebAccessRoleSeeder>();
        services.AddScoped<WebUserAdministration>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddSingleton<LoginAttemptLimiter>();
        services.AddScoped<AntiforgeryEndpointFilter>();
        services.AddScoped<LoopbackOnlyEndpointFilter>();
        return services;
    }

    internal static async Task InitializeWebAccessSecurityAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var webAccessDb = scope.ServiceProvider.GetRequiredService<WebAccessDbContext>();
        var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await webAccessDb.Database.MigrateAsync(cancellationToken);
        await auditDb.Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<WebAccessRoleSeeder>().SeedAsync(cancellationToken);
    }
}

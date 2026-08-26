using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NasForWindows.Api.Features.WebAccess.Persistence;

internal sealed class WebAccessDbContext(DbContextOptions<WebAccessDbContext> options)
    : IdentityDbContext<WebUser, IdentityRole, string>(options)
{
    internal DbSet<BootstrapTokenRecord> BootstrapTokens => Set<BootstrapTokenRecord>();

    internal DbSet<BootstrapState> BootstrapStates => Set<BootstrapState>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<WebUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(128).IsRequired();
            entity.Property(user => user.IsEnabled).IsRequired();
        });

        builder.Entity<BootstrapTokenRecord>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(token => token.TokenHash).IsUnique();
        });

        builder.Entity<BootstrapState>(entity =>
        {
            entity.HasKey(state => state.Id);
            entity.ToTable(table => table.HasCheckConstraint("CK_BootstrapState_Singleton", "Id = 1"));
        });
    }
}

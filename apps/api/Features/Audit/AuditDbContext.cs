using Microsoft.EntityFrameworkCore;

namespace NasForWindows.Api.Features.Audit;

internal sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    internal DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(auditEvent => auditEvent.Id);
            entity.Property(auditEvent => auditEvent.Action).HasMaxLength(128).IsRequired();
            entity.Property(auditEvent => auditEvent.TargetType).HasMaxLength(64);
            entity.Property(auditEvent => auditEvent.TargetId).HasMaxLength(128);
            entity.Property(auditEvent => auditEvent.Outcome).HasMaxLength(32).IsRequired();
            entity.Property(auditEvent => auditEvent.ActorUserId).HasMaxLength(64);
            entity.Property(auditEvent => auditEvent.ActorName).HasMaxLength(256);
            entity.Property(auditEvent => auditEvent.SourceIp).HasMaxLength(64);
            entity.Property(auditEvent => auditEvent.CorrelationId).HasMaxLength(128).IsRequired();
            entity.Property(auditEvent => auditEvent.Details).HasMaxLength(2048);
            entity.HasIndex(auditEvent => auditEvent.OccurredAtUtc);
        });
    }
}

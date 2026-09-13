using Microsoft.EntityFrameworkCore;
using BKM.Utility.Domain.Features.AppLog.Entities;

namespace BKM.Utility.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the AppLog feature.
/// Owns: AppLogs, LogRetentionPolicies.
/// Completely independent — no other feature's tables are present.
/// Connection string key: "AppLog" (falls back to "DefaultConnection").
/// </summary>
public sealed class AppLogDbContext(DbContextOptions<AppLogDbContext> options)
    : DbContext(options)
{
    public DbSet<AppLogEntry>        AppLogs              => Set<AppLogEntry>();
    public DbSet<LogRetentionPolicy> LogRetentionPolicies => Set<LogRetentionPolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppLogEntry>(e =>
        {
            e.ToTable("AppLogs", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Level).HasMaxLength(20).IsRequired();
            e.Property(x => x.Message).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Exception).HasMaxLength(8000);
            e.Property(x => x.TraceId).HasMaxLength(64);
            e.Property(x => x.Feature).HasMaxLength(100);
            e.Property(x => x.MachineName).HasMaxLength(256);
            e.Property(x => x.Environment).HasMaxLength(50);
            e.Property(x => x.Properties).HasColumnType("nvarchar(max)");
            e.Property(x => x.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.Level).HasDatabaseName("IX_AppLogs_Level");
            e.HasIndex(x => x.Timestamp).HasDatabaseName("IX_AppLogs_Timestamp");
            e.HasIndex(x => x.Feature).HasDatabaseName("IX_AppLogs_Feature");
        });

        modelBuilder.Entity<LogRetentionPolicy>(e =>
        {
            e.ToTable("LogRetentionPolicies", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Feature).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.DbRetentionDays).IsRequired();
            e.Property(x => x.FileRetentionDays).IsRequired();
            e.Property(x => x.LastUpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.Feature)
             .IsUnique()
             .HasDatabaseName("UX_LogRetentionPolicies_Feature");
        });
    }
}

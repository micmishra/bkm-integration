using Microsoft.EntityFrameworkCore;
using BKM.Utility.Domain.Features.Email.Entities;

namespace BKM.Utility.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Email feature.
/// Owns: EmailConfigs, EmailTemplates, EmailAuditLogs.
/// Completely independent — no other feature's tables are present.
/// Connection string key: "Email" (falls back to "DefaultConnection").
/// </summary>
public sealed class EmailDbContext(DbContextOptions<EmailDbContext> options)
    : DbContext(options)
{
    public DbSet<EmailConfig>   EmailConfigs   => Set<EmailConfig>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailAuditLog> EmailAuditLogs => Set<EmailAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EmailConfig>(e =>
        {
            e.ToTable("EmailConfigs", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Host).HasMaxLength(500).IsRequired();
            e.Property(x => x.Username).HasMaxLength(500).IsRequired();
            e.Property(x => x.PasswordCipher).HasMaxLength(2000).IsRequired();
            e.Property(x => x.FromAddress).HasMaxLength(500).IsRequired();
            e.Property(x => x.FromName).HasMaxLength(200).IsRequired();
            e.Property(x => x.TlsMode).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.IsActive).HasDatabaseName("IX_EmailConfigs_IsActive");
        });

        modelBuilder.Entity<EmailTemplate>(e =>
        {
            e.ToTable("EmailTemplates", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_EmailTemplates_Name");
            e.Property(x => x.Subject).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.IsActive).HasDatabaseName("IX_EmailTemplates_IsActive");
        });

        modelBuilder.Entity<EmailAuditLog>(e =>
        {
            e.ToTable("EmailAuditLogs", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.ToAddress).HasMaxLength(500).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(1000).IsRequired();
            e.Property(x => x.TemplateName).HasMaxLength(200);
            e.Property(x => x.ErrorMessage).HasMaxLength(4000);
            e.Property(x => x.SentAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.SentAt).HasDatabaseName("IX_EmailAuditLogs_SentAt");
            e.HasIndex(x => x.Success).HasDatabaseName("IX_EmailAuditLogs_Success");
        });
    }
}

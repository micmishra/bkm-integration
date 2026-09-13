using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BKM.Integration.Domain.Features.AppLog.Entities;
using BKM.Integration.Domain.Features.Auth.Entities;
using BKM.Integration.Domain.Features.Email.Entities;
using BKM.Integration.Domain.Features.Feed.Entities;
using BKM.Integration.Domain.Features.FileIngestion.Entities;
using BKM.Integration.Domain.Features.UrlShortener.Entities;

namespace BKM.Integration.Infrastructure.Persistence;

/// <summary>
/// Single EF Core DbContext for the application.
/// Inherits IdentityDbContext to provision all ASP.NET Core Identity tables automatically.
/// Each feature's entity configuration lives in its own region below.
/// When a new feature is added, add its entity config in a clearly labelled region.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, string>(options)
{
    // ── UrlShortener feature ───────────────────────────────────────────────────
    public DbSet<ShortenedUrl>  ShortenedUrls => Set<ShortenedUrl>();
    public DbSet<UrlCacheEntry> UrlCache      => Set<UrlCacheEntry>();

    // ── Feed feature ─────────────────────────────────────────────────────────
    public DbSet<Post>          Posts      => Set<Post>();
    public DbSet<Follow>        Follows    => Set<Follow>();
    public DbSet<UserFeedEntry> UserFeeds  => Set<UserFeedEntry>();

    // ── AppLog feature ─────────────────────────────────────────────────────────
    public DbSet<AppLogEntry>        AppLogs              => Set<AppLogEntry>();
    public DbSet<LogRetentionPolicy> LogRetentionPolicies => Set<LogRetentionPolicy>();

    // ── Email feature ──────────────────────────────────────────────────────────
    public DbSet<EmailConfig>   EmailConfigs   => Set<EmailConfig>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailAuditLog> EmailAuditLogs => Set<EmailAuditLog>();

    // ── Auth feature ──────────────────────────────────────────────────────────
    public DbSet<Permission>     Permissions     => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken>   RefreshTokens   => Set<RefreshToken>();

    // ── FileIngestion feature ──────────────────────────────────────────────────────
    public DbSet<IngestionBatch>  IngestionBatches  => Set<IngestionBatch>();
    public DbSet<IngestedRecord>  IngestedRecords   => Set<IngestedRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);  // ← MUST be first: provisions all Identity tables

        // ── UrlShortener: cache table ──────────────────────────────────────────
        modelBuilder.Entity<UrlCacheEntry>(e =>
        {
            e.ToTable("UrlCache", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(449).IsRequired();
            e.Property(x => x.Value).IsRequired();
            e.Property(x => x.ExpiresAtTime).IsRequired();
            e.Property(x => x.SlidingExpirationInSeconds);
            e.Property(x => x.AbsoluteExpiration);
            e.HasIndex(x => x.ExpiresAtTime).HasDatabaseName("Index_ExpiresAtTime");
        });

        // ── UrlShortener: main table ───────────────────────────────────────────
        modelBuilder.Entity<ShortenedUrl>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn(seed: 1, increment: 1);
            e.Property(x => x.Code).HasMaxLength(12).IsRequired();
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IX_ShortenedUrls_Code");
            e.Property(x => x.OriginalUrl).HasMaxLength(2048).IsRequired();
            e.HasIndex(x => x.OriginalUrl).HasDatabaseName("IX_ShortenedUrls_OriginalUrl");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── AppLog: structured log table ──────────────────────────────────────
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

        // ── AppLog: retention policy table ────────────────────────────────────
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

        // ── Email: config table ────────────────────────────────────────────────
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

        // ── Email: template table ──────────────────────────────────────────────
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

        // ── Email: audit log table ─────────────────────────────────────────────
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

        // ── Feed feature: Post ────────────────────────────────────────────────
        modelBuilder.Entity<Post>(e =>
        {
            e.ToTable("Posts", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.UserId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Body).HasMaxLength(500).IsRequired();
            e.Property(x => x.MediaUrl).HasMaxLength(1000);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => new { x.UserId, x.CreatedAt })
             .HasDatabaseName("IX_Posts_UserId_CreatedAt")
             .IsDescending(false, true);
        });

        // ── Feed feature: Follow ───────────────────────────────────────────────
        modelBuilder.Entity<Follow>(e =>
        {
            e.ToTable("Follows", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.FollowerId).HasMaxLength(100).IsRequired();
            e.Property(x => x.FolloweeId).HasMaxLength(100).IsRequired();
            e.Property(x => x.FollowedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => new { x.FollowerId, x.FolloweeId })
             .IsUnique()
             .HasDatabaseName("UX_Follows_FollowerId_FolloweeId");
            e.HasIndex(x => x.FollowerId).HasDatabaseName("IX_Follows_FollowerId");
            e.HasIndex(x => x.FolloweeId).HasDatabaseName("IX_Follows_FolloweeId");
        });

        // ── Feed feature: UserFeedEntry ────────────────────────────────────────
        modelBuilder.Entity<UserFeedEntry>(e =>
        {
            e.ToTable("UserFeeds", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.OwnerId).HasMaxLength(100).IsRequired();
            e.Property(x => x.PostId).IsRequired();
            e.Property(x => x.AuthorId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Body).HasMaxLength(500).IsRequired();
            e.Property(x => x.MediaUrl).HasMaxLength(1000);
            e.Property(x => x.PostedAt).HasColumnType("datetime2").IsRequired();
            e.HasIndex(x => new { x.OwnerId, x.PostedAt })
             .HasDatabaseName("IX_UserFeeds_OwnerId_PostedAt")
             .IsDescending(false, true);
        });

        // ── Auth: Permission table ─────────────────────────────────────────────
        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("Permissions", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_Permissions_Name");
            e.Property(x => x.Resource).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
        });

        // ── Auth: RolePermission table ─────────────────────────────────────────
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermissions", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.RoleId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.RoleId, x.PermissionId })
             .IsUnique()
             .HasDatabaseName("UX_RolePermissions_RoleId_PermissionId");
        });

        // ── Auth: RefreshToken table ───────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.TokenHash).HasDatabaseName("IX_RefreshTokens_TokenHash");
            e.HasIndex(x => x.UserId).HasDatabaseName("IX_RefreshTokens_UserId");
            e.Property(x => x.DeviceInfo).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── FileIngestion: IngestionBatch ──────────────────────────────────────────
        modelBuilder.Entity<IngestionBatch>(e =>
        {
            e.ToTable("IngestionBatches", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();  // Guid set by application
            e.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileType).HasMaxLength(20).IsRequired();
            e.Property(x => x.RecordType).HasMaxLength(200);
            e.Property(x => x.ParseOptions).HasColumnType("nvarchar(max)");
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(4000);
            e.Property(x => x.StartedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasIndex(x => x.Status).HasDatabaseName("IX_IngestionBatches_Status");
            e.HasIndex(x => x.StartedAt).HasDatabaseName("IX_IngestionBatches_StartedAt");
        });

        // ── FileIngestion: IngestedRecord (master table for all file types) ────────
        modelBuilder.Entity<IngestedRecord>(e =>
        {
            e.ToTable("IngestedRecords", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.BatchId).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileType).HasMaxLength(20).IsRequired();
            e.Property(x => x.RecordType).HasMaxLength(200);
            e.Property(x => x.Payload).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.IngestedAt).HasDefaultValueSql("GETUTCDATE()");

            // Composite index for batch + row lookups
            e.HasIndex(x => new { x.BatchId, x.RowNumber }).HasDatabaseName("IX_IngestedRecords_BatchId_RowNumber");
            // Index for dedup hash checks
            e.HasIndex(x => new { x.BatchId, x.PayloadHash }).HasDatabaseName("IX_IngestedRecords_BatchId_PayloadHash");
            // Index for file type + record type queries
            e.HasIndex(x => new { x.FileType, x.RecordType }).HasDatabaseName("IX_IngestedRecords_FileType_RecordType");
            // Covering index for IngestedAt (time-range queries)
            e.HasIndex(x => x.IngestedAt).HasDatabaseName("IX_IngestedRecords_IngestedAt");
        });
    }
}

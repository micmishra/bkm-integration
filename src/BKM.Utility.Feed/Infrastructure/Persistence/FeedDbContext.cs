using Microsoft.EntityFrameworkCore;
using BKM.Utility.Domain.Features.Feed.Entities;

namespace BKM.Utility.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Feed feature.
/// Owns: Posts, Follows, UserFeeds.
/// Completely independent — no other feature's tables are present.
/// Connection string key: "Feed" (falls back to "DefaultConnection").
/// </summary>
public sealed class FeedDbContext(DbContextOptions<FeedDbContext> options)
    : DbContext(options)
{
    public DbSet<Post>          Posts     => Set<Post>();
    public DbSet<Follow>        Follows   => Set<Follow>();
    public DbSet<UserFeedEntry> UserFeeds => Set<UserFeedEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}

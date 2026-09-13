using Microsoft.EntityFrameworkCore;
using BKM.Utility.Domain.Features.UrlShortener.Entities;

namespace BKM.Utility.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the shared distributed cache.
/// Owns: dbo.BkmCache — the SQL Server backing table for IDistributedCache.
///
/// This context is completely independent of every feature context.
/// Any feature that needs ICacheService&lt;T&gt; only depends on this context,
/// not on UrlShortenerDbContext or any other feature.
///
/// Connection string key: "Cache" (falls back to "DefaultConnection").
/// </summary>
public sealed class CacheDbContext(DbContextOptions<CacheDbContext> options)
    : DbContext(options)
{
    /// <summary>
    /// The backing table for Microsoft.Extensions.Caching.SqlServer (IDistributedCache).
    /// Exposed as a DbSet so EF migrations can create and manage the schema.
    /// The distributed cache provider reads/writes this table directly via ADO.NET;
    /// this DbSet is only used to provision the schema through migrations.
    /// </summary>
    public DbSet<UrlCacheEntry> BkmCache => Set<UrlCacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Schema must exactly match what Microsoft.Extensions.Caching.SqlServer expects.
        modelBuilder.Entity<UrlCacheEntry>(e =>
        {
            e.ToTable("BkmCache", "dbo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(449).IsRequired();
            e.Property(x => x.Value).IsRequired();
            e.Property(x => x.ExpiresAtTime).IsRequired();
            e.Property(x => x.SlidingExpirationInSeconds);
            e.Property(x => x.AbsoluteExpiration);
            e.HasIndex(x => x.ExpiresAtTime).HasDatabaseName("Index_ExpiresAtTime");
        });
    }
}

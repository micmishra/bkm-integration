using Microsoft.EntityFrameworkCore;
using BKM.Utility.Domain.Features.UrlShortener.Entities;

namespace BKM.Utility.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the UrlShortener feature.
/// Owns: ShortenedUrls only.
/// The distributed cache table (dbo.BkmCache) is owned by CacheDbContext.
/// Connection string key: "UrlShortener" (falls back to "DefaultConnection").
/// </summary>
public sealed class UrlShortenerDbContext(DbContextOptions<UrlShortenerDbContext> options)
    : DbContext(options)
{
    public DbSet<ShortenedUrl> ShortenedUrls => Set<ShortenedUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}

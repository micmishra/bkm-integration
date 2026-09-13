using Microsoft.EntityFrameworkCore;
using BKM.Utility.Domain.Features.UrlShortener.Entities;
using BKM.Utility.Domain.Features.UrlShortener.Interfaces;
using BKM.Utility.Infrastructure.Persistence;

namespace BKM.Utility.Infrastructure.Features.UrlShortener.Persistence;

public sealed class UrlRepository(UrlShortenerDbContext db) : IUrlRepository
{
    public async Task SaveAsync(ShortenedUrl entity, CancellationToken ct = default)
    {
        if (entity.Id == 0)
            db.ShortenedUrls.Add(entity);
        else
            db.ShortenedUrls.Update(entity);

        await db.SaveChangesAsync(ct);
    }

    public Task<ShortenedUrl?> FindByUrlAsync(string originalUrl, CancellationToken ct = default)
        => db.ShortenedUrls
             .AsNoTracking()
             .FirstOrDefaultAsync(x => x.OriginalUrl == originalUrl, ct);

    public Task<ShortenedUrl?> FindByCodeAsync(string code, CancellationToken ct = default)
        => db.ShortenedUrls
             .AsNoTracking()
             .FirstOrDefaultAsync(x => x.Code == code, ct);
}

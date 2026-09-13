using BKM.Integration.Domain.Features.UrlShortener.Entities;

namespace BKM.Integration.Domain.Features.UrlShortener.Interfaces;

public interface IUrlRepository
{
    Task SaveAsync(ShortenedUrl entity, CancellationToken ct = default);
    Task<ShortenedUrl?> FindByUrlAsync(string originalUrl, CancellationToken ct = default);
    Task<ShortenedUrl?> FindByCodeAsync(string code, CancellationToken ct = default);
}

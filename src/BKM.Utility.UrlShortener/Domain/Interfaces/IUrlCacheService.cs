namespace BKM.Utility.Domain.Features.UrlShortener.Interfaces;

public interface IUrlCacheService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
}

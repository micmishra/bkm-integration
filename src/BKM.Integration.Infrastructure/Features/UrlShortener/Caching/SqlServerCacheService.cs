using Microsoft.Extensions.Caching.Distributed;
using BKM.Integration.Domain.Features.UrlShortener.Interfaces;

namespace BKM.Integration.Infrastructure.Features.UrlShortener.Caching;

public sealed class SqlServerCacheService(IDistributedCache cache) : IUrlCacheService
{
    private static readonly DistributedCacheEntryOptions CacheOpts = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
        => cache.GetStringAsync(key, ct);

    public Task SetAsync(string key, string value, CancellationToken ct = default)
        => cache.SetStringAsync(key, value, CacheOpts, ct);
}

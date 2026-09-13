namespace BKM.Utility.Domain.Shared.Interfaces;

/// <summary>
/// Platform-wide distributed cache abstraction.
/// Any feature can inject ICacheService&lt;T&gt; for strongly-typed, JSON-serialised caching
/// backed by the SQL Server distributed cache (dbo.BkmCache table).
///
/// Key conventions (adopted by each feature):
///   UrlShortener   — "url:{code}"           TTL 24h
///   AppLog queries — "log:query:{hash}"      TTL 30s   (short, logs change frequently)
///
/// All methods are fire-friendly: GetAsync returns null on miss or error.
/// RemoveAsync and SetAsync silently swallow cache-layer errors so they never
/// break the main request path.
/// </summary>
public interface ICacheService<T> where T : class
{
    /// <summary>Returns the cached value, or null on miss.</summary>
    Task<T?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Stores a value with the given absolute TTL.</summary>
    Task SetAsync(string key, T value, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Removes a cached entry. No-op if the key does not exist.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Returns true if the key exists in the cache.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}

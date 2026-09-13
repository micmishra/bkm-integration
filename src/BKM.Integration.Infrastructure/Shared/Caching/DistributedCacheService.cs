using System.Text.Json;
using BKM.Integration.Domain.Shared.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BKM.Integration.Infrastructure.Shared.Caching;

/// <summary>
/// Generic distributed cache service backed by SQL Server (Microsoft.Extensions.Caching.SqlServer).
/// Values are JSON-serialised with System.Text.Json — no extra NuGet packages needed.
///
/// Error handling:
///   GetAsync   — returns null on any cache error (miss or fault) so callers always fall through to DB.
///   SetAsync   — swallows errors; a write failure never breaks the request.
///   RemoveAsync — swallows errors.
///   ExistsAsync — returns false on error.
///
/// Thread-safety: IDistributedCache implementations are thread-safe.
/// </summary>
public sealed class DistributedCacheService<T>(
    IDistributedCache cache,
    ILogger<DistributedCacheService<T>> logger) : ICacheService<T>
    where T : class
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false
    };

    public async Task<T?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await cache.GetAsync(key, ct);
            if (bytes is null or { Length: 0 }) return null;
            return JsonSerializer.Deserialize<T>(bytes, JsonOpts);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache GET failed for key '{Key}'. Falling through to source.", key);
            return null;
        }
    }

    public async Task SetAsync(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOpts);
            var opts  = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            await cache.SetAsync(key, bytes, opts, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache SET failed for key '{Key}'. Continuing without cache.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache REMOVE failed for key '{Key}'.", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await cache.GetAsync(key, ct);
            return bytes is not null && bytes.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}

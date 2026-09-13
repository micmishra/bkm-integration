using Microsoft.Extensions.Configuration;
using BKM.Integration.Application.Features.UrlShortener.Interfaces;
using BKM.Integration.Domain.Features.UrlShortener.Entities;
using BKM.Integration.Domain.Features.UrlShortener.Interfaces;
using BKM.Integration.Domain.Shared.Interfaces;

namespace BKM.Integration.Application.Features.UrlShortener.Services;

/// <summary>
/// Orchestrates IUrlRepository + ICacheService&lt;string&gt; to shorten and resolve URLs.
///
/// Shorten flow:
///   1. Dedup check in repository
///   2. INSERT row → get IDENTITY Id → Code = Base62(Id) → UPDATE
///   3. Populate cache: key = "url:{code}", value = original URL, TTL 24h
///
/// Resolve flow (hot path):
///   1. Cache hit  → return immediately (no DB touch)
///   2. Cache miss → repository lookup → back-fill cache → return
/// </summary>
public sealed class UrlShortenerService(
    IUrlRepository repository,
    ICacheService<string> cache,
    IConfiguration config) : IUrlShortenerService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private static string CacheKey(string code) => $"url:{code}";

    public async Task<ShortenedUrlResult> ShortenAsync(string originalUrl, CancellationToken ct = default)
    {
        var existing = await repository.FindByUrlAsync(originalUrl, ct);
        if (existing is not null)
            return ToResult(existing);

        var entry = new ShortenedUrl
        {
            OriginalUrl = originalUrl,
            Code        = "__pending__",
            CreatedAt   = DateTime.UtcNow
        };

        await repository.SaveAsync(entry, ct);              // Id populated by IDENTITY
        entry.Code = Base62Encoder.Encode(entry.Id);
        await repository.SaveAsync(entry, ct);              // persist final code

        await cache.SetAsync(CacheKey(entry.Code), entry.OriginalUrl, CacheTtl, ct);

        return ToResult(entry);
    }

    public async Task<string?> ResolveAsync(string code, CancellationToken ct = default)
    {
        var cached = await cache.GetAsync(CacheKey(code), ct);
        if (cached is not null) return cached;

        var record = await repository.FindByCodeAsync(code, ct);
        if (record is null) return null;

        await cache.SetAsync(CacheKey(code), record.OriginalUrl, CacheTtl, ct);
        return record.OriginalUrl;
    }

    public string BuildShortUrl(string code)
    {
        var baseUrl = config["BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5000";
        return $"{baseUrl}/{code}";
    }

    private static ShortenedUrlResult ToResult(ShortenedUrl e) => new()
    {
        Id          = e.Id,
        Code        = e.Code,
        OriginalUrl = e.OriginalUrl,
        CreatedAt   = e.CreatedAt
    };
}

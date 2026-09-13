using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BKM.Integration.Application.Features.AppLog.DTOs;
using BKM.Integration.Application.Features.AppLog.Interfaces;
using BKM.Integration.Domain.Features.AppLog.Entities;
using BKM.Integration.Domain.Features.AppLog.Interfaces;
using BKM.Integration.Domain.Shared.Interfaces;

namespace BKM.Integration.Application.Features.AppLog.Services;

/// <summary>
/// Queries structured logs with short-lived distributed cache (30 s TTL).
///
/// Cache strategy:
///   - Key = "log:query:{SHA256 of JSON-serialised request}"
///   - TTL = 30 seconds — short enough that log data feels live, long enough to absorb burst reads
///   - On cache hit, DB is never touched
///   - Cache is bypassed (but not invalidated) when the DB write occurs in DatabaseLogSink;
///     the short TTL means stale pages auto-expire quickly
/// </summary>
public sealed class AppLogService(
    IAppLogRepository repository,
    ICacheService<AppLogPageResult> cache) : IAppLogService
{
    private static readonly TimeSpan QueryCacheTtl = TimeSpan.FromSeconds(30);

    public async Task<(IReadOnlyList<AppLogEntryDto> Items, int TotalCount)> QueryAsync(
        AppLogQueryRequest request,
        CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var page     = Math.Max(request.Page, 1);

        var cacheKey = BuildCacheKey(request, page, pageSize);

        // Cache-aside: try cache first
        var cached = await cache.GetAsync(cacheKey, ct);
        if (cached is not null)
            return (cached.Items, cached.TotalCount);

        // Cache miss — query DB
        var (items, total) = await FetchAsync(request, page, pageSize, ct);
        var dtos = items.Select(Map).ToList();

        // Back-fill cache (fire-and-forget safe — SetAsync swallows errors)
        await cache.SetAsync(cacheKey, new AppLogPageResult { Items = dtos, TotalCount = total },
            QueryCacheTtl, ct);

        return (dtos, total);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<(IReadOnlyList<AppLogEntry> Items, int Total)> FetchAsync(
        AppLogQueryRequest req, int page, int pageSize, CancellationToken ct)
    {
        var items = await repository.QueryAsync(
            req.Level, req.Feature, req.From, req.To, page, pageSize, ct);

        var total = await repository.CountAsync(
            req.Level, req.Feature, req.From, req.To, ct);

        return (items, total);
    }

    private static string BuildCacheKey(AppLogQueryRequest req, int page, int pageSize)
    {
        // Stable JSON fingerprint → SHA-256 → hex prefix key
        var fingerprint = JsonSerializer.Serialize(new
        {
            req.Level, req.Feature,
            From = req.From?.ToString("O"),
            To   = req.To?.ToString("O"),
            page, pageSize
        });

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
        return $"log:query:{hash[..16]}";   // first 16 hex chars (64-bit) — collision-free in practice
    }

    private static AppLogEntryDto Map(AppLogEntry e) => new()
    {
        Id          = e.Id,
        Level       = e.Level,
        Message     = e.Message,
        Exception   = e.Exception,
        TraceId     = e.TraceId,
        Feature     = e.Feature,
        MachineName = e.MachineName,
        Environment = e.Environment,
        Properties  = e.Properties,
        Timestamp   = e.Timestamp
    };
}

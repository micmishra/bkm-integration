using BKM.Utility.Application.Features.Feed.DTOs;
using BKM.Utility.Application.Features.Feed.Interfaces;
using BKM.Utility.Domain.Features.Feed.Entities;
using BKM.Utility.Domain.Features.Feed.Interfaces;
using BKM.Utility.Domain.Shared.Interfaces;

namespace BKM.Utility.Application.Features.Feed.Services;

/// <summary>
/// Push feed: fan-out on write — pre-computes one row per follower.
/// Read path: serves from the pre-computed UserFeeds table with a 60 s cache.
/// </summary>
public sealed class PushFeedService(
    IFollowRepository followRepository,
    IUserFeedRepository userFeedRepository,
    ICacheService<FeedPageResult> cache) : IPushFeedService
{
    private const int  FeedCap     = 500;
    private const int  CacheEvictPages = 5;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    // ── Fan-out ────────────────────────────────────────────────────────────────

    public async Task FanOutAsync(Post post, CancellationToken ct = default)
    {
        var followers = await followRepository.GetFollowersAsync(post.UserId, ct);
        if (followers.Count == 0)
            return;

        var entries = followers.Select(followerId => new UserFeedEntry
        {
            OwnerId  = followerId,
            PostId   = post.Id,
            AuthorId = post.UserId,
            Body     = post.Body,
            MediaUrl = post.MediaUrl,
            PostedAt = post.CreatedAt
        }).ToList();

        await userFeedRepository.AppendAsync(entries, ct);

        // Trim each follower's feed to FeedCap entries and evict cached pages 1-5
        foreach (var followerId in followers)
        {
            await userFeedRepository.PurgeOldEntriesAsync(followerId, FeedCap, ct);

            for (var p = 1; p <= CacheEvictPages; p++)
                await cache.RemoveAsync(PushCacheKey(followerId, p), ct);
        }
    }

    // ── Read ───────────────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<FeedItemDto> Items, int TotalCount)> GetFeedAsync(
        FeedQueryRequest request,
        CancellationToken ct = default)
    {
        var page     = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var cacheKey = PushCacheKey(request.UserId, page);

        var cached = await cache.GetAsync(cacheKey, ct);
        if (cached is not null)
            return (cached.Items, cached.TotalCount);

        var entries = await userFeedRepository.GetFeedAsync(request.UserId, page, pageSize, ct);
        var total   = await userFeedRepository.CountAsync(request.UserId, ct);

        var items = entries.Select(e => new FeedItemDto
        {
            PostId   = e.PostId,
            AuthorId = e.AuthorId,
            Body     = e.Body,
            MediaUrl = e.MediaUrl,
            PostedAt = e.PostedAt
        }).ToList();

        await cache.SetAsync(cacheKey, new FeedPageResult { Items = items, TotalCount = total }, CacheTtl, ct);

        return (items, total);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string PushCacheKey(string userId, int page) => $"push:feed:{userId}:p{page}";
}

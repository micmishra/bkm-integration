using BKM.Integration.Application.Features.Feed.DTOs;
using BKM.Integration.Application.Features.Feed.Interfaces;
using BKM.Integration.Domain.Features.Feed.Interfaces;
using BKM.Integration.Domain.Shared.Interfaces;

namespace BKM.Integration.Application.Features.Feed.Services;

/// <summary>
/// Pull feed: fan-in on read — queries posts from all followees on demand.
/// Results are cached for 30 s.
/// </summary>
public sealed class PullFeedService(
    IFollowRepository followRepository,
    IPostRepository postRepository,
    ICacheService<FeedPageResult> cache) : IPullFeedService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public async Task<(IReadOnlyList<FeedItemDto> Items, int TotalCount)> GetFeedAsync(
        FeedQueryRequest request,
        CancellationToken ct = default)
    {
        var page     = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var cacheKey = $"pull:feed:{request.UserId}:p{page}";

        var cached = await cache.GetAsync(cacheKey, ct);
        if (cached is not null)
            return (cached.Items, cached.TotalCount);

        var followeeIds = await followRepository.GetFolloweesAsync(request.UserId, ct);
        if (followeeIds.Count == 0)
            return ([], 0);

        // Aggregate posts from all followees: fetch a large window then paginate in-memory
        // to avoid N+1 queries while keeping a single DB call per followee simple.
        var allPosts = new List<(long PostId, string AuthorId, string Body, string? MediaUrl, DateTime PostedAt)>();

        foreach (var followeeId in followeeIds)
        {
            var posts = await postRepository.GetByUserAsync(followeeId, 1, 1000, ct);
            allPosts.AddRange(posts.Select(p => (p.Id, p.UserId, p.Body, p.MediaUrl, p.CreatedAt)));
        }

        var sorted = allPosts
            .OrderByDescending(p => p.PostedAt)
            .ToList();

        var total  = sorted.Count;
        var paged  = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var items = paged.Select(p => new FeedItemDto
        {
            PostId   = p.PostId,
            AuthorId = p.AuthorId,
            Body     = p.Body,
            MediaUrl = p.MediaUrl,
            PostedAt = p.PostedAt
        }).ToList();

        await cache.SetAsync(cacheKey, new FeedPageResult { Items = items, TotalCount = total }, CacheTtl, ct);

        return (items, total);
    }
}

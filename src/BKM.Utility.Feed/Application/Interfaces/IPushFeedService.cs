using BKM.Utility.Application.Features.Feed.DTOs;
using BKM.Utility.Domain.Features.Feed.Entities;

namespace BKM.Utility.Application.Features.Feed.Interfaces;

public interface IPushFeedService
{
    Task FanOutAsync(Post post, CancellationToken ct = default);
    Task<(IReadOnlyList<FeedItemDto> Items, int TotalCount)> GetFeedAsync(FeedQueryRequest request, CancellationToken ct = default);
}

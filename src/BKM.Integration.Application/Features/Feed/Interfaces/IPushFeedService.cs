using BKM.Integration.Application.Features.Feed.DTOs;
using BKM.Integration.Domain.Features.Feed.Entities;

namespace BKM.Integration.Application.Features.Feed.Interfaces;

public interface IPushFeedService
{
    Task FanOutAsync(Post post, CancellationToken ct = default);
    Task<(IReadOnlyList<FeedItemDto> Items, int TotalCount)> GetFeedAsync(FeedQueryRequest request, CancellationToken ct = default);
}

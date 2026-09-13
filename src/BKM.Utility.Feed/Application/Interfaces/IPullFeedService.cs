using BKM.Utility.Application.Features.Feed.DTOs;

namespace BKM.Utility.Application.Features.Feed.Interfaces;

public interface IPullFeedService
{
    Task<(IReadOnlyList<FeedItemDto> Items, int TotalCount)> GetFeedAsync(FeedQueryRequest request, CancellationToken ct = default);
}

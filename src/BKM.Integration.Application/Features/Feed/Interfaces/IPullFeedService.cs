using BKM.Integration.Application.Features.Feed.DTOs;

namespace BKM.Integration.Application.Features.Feed.Interfaces;

public interface IPullFeedService
{
    Task<(IReadOnlyList<FeedItemDto> Items, int TotalCount)> GetFeedAsync(FeedQueryRequest request, CancellationToken ct = default);
}

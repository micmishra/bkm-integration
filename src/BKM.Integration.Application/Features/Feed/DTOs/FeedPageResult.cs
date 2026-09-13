namespace BKM.Integration.Application.Features.Feed.DTOs;

public sealed class FeedPageResult
{
    public IReadOnlyList<FeedItemDto> Items      { get; init; } = [];
    public int                        TotalCount { get; init; }
}

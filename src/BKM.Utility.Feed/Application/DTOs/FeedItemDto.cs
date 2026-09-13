namespace BKM.Utility.Application.Features.Feed.DTOs;

public sealed class FeedItemDto
{
    public long     PostId   { get; init; }
    public string   AuthorId { get; init; } = string.Empty;
    public string   Body     { get; init; } = string.Empty;
    public string?  MediaUrl { get; init; }
    public DateTime PostedAt { get; init; }
}

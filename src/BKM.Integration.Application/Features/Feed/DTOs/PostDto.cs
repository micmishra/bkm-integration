namespace BKM.Integration.Application.Features.Feed.DTOs;

public sealed class PostDto
{
    public long     Id        { get; init; }
    public string   UserId    { get; init; } = string.Empty;
    public string   Body      { get; init; } = string.Empty;
    public string?  MediaUrl  { get; init; }
    public DateTime CreatedAt { get; init; }
}

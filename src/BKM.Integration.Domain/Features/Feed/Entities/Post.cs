namespace BKM.Integration.Domain.Features.Feed.Entities;

public sealed class Post
{
    public long     Id        { get; set; }
    public string   UserId    { get; set; } = string.Empty;  // author
    public string   Body      { get; set; } = string.Empty;
    public string?  MediaUrl  { get; set; }
    public DateTime CreatedAt { get; set; }
}

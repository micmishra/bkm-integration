namespace BKM.Integration.Domain.Features.Feed.Entities;

/// <summary>Pre-computed feed row written during fan-out (push model).</summary>
public sealed class UserFeedEntry
{
    public long     Id       { get; set; }
    public string   OwnerId  { get; set; } = string.Empty;  // follower who owns this feed slot
    public long     PostId   { get; set; }
    public string   AuthorId { get; set; } = string.Empty;
    public string   Body     { get; set; } = string.Empty;
    public string?  MediaUrl { get; set; }
    public DateTime PostedAt { get; set; }
}

namespace BKM.Utility.Domain.Features.Feed.Entities;

public sealed class Follow
{
    public long     Id         { get; set; }
    public string   FollowerId { get; set; } = string.Empty;
    public string   FolloweeId { get; set; } = string.Empty;
    public DateTime FollowedAt { get; set; }
}

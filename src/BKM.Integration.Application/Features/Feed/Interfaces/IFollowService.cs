namespace BKM.Integration.Application.Features.Feed.Interfaces;

public interface IFollowService
{
    Task FollowAsync(string followerId, string followeeId, CancellationToken ct = default);
    Task UnfollowAsync(string followerId, string followeeId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetFolloweesAsync(string userId, CancellationToken ct = default);
}

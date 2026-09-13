using BKM.Integration.Domain.Features.Feed.Entities;

namespace BKM.Integration.Domain.Features.Feed.Interfaces;

public interface IFollowRepository
{
    Task FollowAsync(Follow follow, CancellationToken ct = default);
    Task UnfollowAsync(string followerId, string followeeId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetFollowersAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetFolloweesAsync(string userId, CancellationToken ct = default);
    Task<bool> IsFollowingAsync(string followerId, string followeeId, CancellationToken ct = default);
}

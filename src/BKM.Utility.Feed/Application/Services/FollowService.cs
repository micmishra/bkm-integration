using BKM.Utility.Application.Features.Feed.Interfaces;
using BKM.Utility.Domain.Features.Feed.Entities;
using BKM.Utility.Domain.Features.Feed.Interfaces;

namespace BKM.Utility.Application.Features.Feed.Services;

/// <summary>
/// Manages follow/unfollow relationships and exposes followee queries.
/// </summary>
public sealed class FollowService(IFollowRepository followRepository) : IFollowService
{
    public async Task FollowAsync(string followerId, string followeeId, CancellationToken ct = default)
    {
        var follow = new Follow
        {
            FollowerId = followerId,
            FolloweeId = followeeId,
            FollowedAt = DateTime.UtcNow
        };
        await followRepository.FollowAsync(follow, ct);
    }

    public Task UnfollowAsync(string followerId, string followeeId, CancellationToken ct = default)
        => followRepository.UnfollowAsync(followerId, followeeId, ct);

    public Task<IReadOnlyList<string>> GetFolloweesAsync(string userId, CancellationToken ct = default)
        => followRepository.GetFolloweesAsync(userId, ct);
}

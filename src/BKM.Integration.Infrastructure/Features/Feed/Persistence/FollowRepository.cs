using Microsoft.EntityFrameworkCore;
using BKM.Integration.Domain.Features.Feed.Entities;
using BKM.Integration.Domain.Features.Feed.Interfaces;
using BKM.Integration.Infrastructure.Persistence;

namespace BKM.Integration.Infrastructure.Features.Feed.Persistence;

public sealed class FollowRepository(AppDbContext db) : IFollowRepository
{
    public async Task FollowAsync(Follow follow, CancellationToken ct = default)
    {
        db.Follows.Add(follow);
        await db.SaveChangesAsync(ct);
    }

    public async Task UnfollowAsync(string followerId, string followeeId, CancellationToken ct = default)
    {
        var row = await db.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, ct);
        if (row is null) return;
        db.Follows.Remove(row);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetFollowersAsync(string userId, CancellationToken ct = default)
        => await db.Follows
               .AsNoTracking()
               .Where(f => f.FolloweeId == userId)
               .Select(f => f.FollowerId)
               .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetFolloweesAsync(string userId, CancellationToken ct = default)
        => await db.Follows
               .AsNoTracking()
               .Where(f => f.FollowerId == userId)
               .Select(f => f.FolloweeId)
               .ToListAsync(ct);

    public Task<bool> IsFollowingAsync(string followerId, string followeeId, CancellationToken ct = default)
        => db.Follows
             .AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, ct);
}

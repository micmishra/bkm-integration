using Microsoft.EntityFrameworkCore;
using BKM.Integration.Domain.Features.Feed.Entities;
using BKM.Integration.Domain.Features.Feed.Interfaces;
using BKM.Integration.Infrastructure.Persistence;

namespace BKM.Integration.Infrastructure.Features.Feed.Persistence;

public sealed class UserFeedRepository(AppDbContext db) : IUserFeedRepository
{
    public async Task AppendAsync(IReadOnlyList<UserFeedEntry> entries, CancellationToken ct = default)
    {
        db.UserFeeds.AddRange(entries);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<UserFeedEntry>> GetFeedAsync(
        string ownerId, int page, int pageSize, CancellationToken ct = default)
        => await db.UserFeeds
               .AsNoTracking()
               .Where(e => e.OwnerId == ownerId)
               .OrderByDescending(e => e.PostedAt)
               .Skip((page - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync(ct);

    public Task<int> CountAsync(string ownerId, CancellationToken ct = default)
        => db.UserFeeds.CountAsync(e => e.OwnerId == ownerId, ct);

    public async Task PurgeOldEntriesAsync(string ownerId, int keepCount, CancellationToken ct = default)
    {
        var toDelete = db.UserFeeds
            .Where(x => x.OwnerId == ownerId)
            .OrderByDescending(x => x.PostedAt)
            .Skip(keepCount);

        await toDelete.ExecuteDeleteAsync(ct);
    }
}

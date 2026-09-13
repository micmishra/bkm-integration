using BKM.Utility.Domain.Features.Feed.Entities;

namespace BKM.Utility.Domain.Features.Feed.Interfaces;

public interface IUserFeedRepository
{
    Task AppendAsync(IReadOnlyList<UserFeedEntry> entries, CancellationToken ct = default);
    Task<IReadOnlyList<UserFeedEntry>> GetFeedAsync(string ownerId, int page, int pageSize, CancellationToken ct = default);
    Task<int>  CountAsync(string ownerId, CancellationToken ct = default);
    Task PurgeOldEntriesAsync(string ownerId, int keepCount, CancellationToken ct = default);
}

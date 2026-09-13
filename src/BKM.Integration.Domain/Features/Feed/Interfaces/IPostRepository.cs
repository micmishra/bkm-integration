using BKM.Integration.Domain.Features.Feed.Entities;

namespace BKM.Integration.Domain.Features.Feed.Interfaces;

public interface IPostRepository
{
    Task<Post>                    SaveAsync(Post post, CancellationToken ct = default);
    Task<Post?>                   GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<Post>>     GetByUserAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<int>                     CountByUserAsync(string userId, CancellationToken ct = default);
}

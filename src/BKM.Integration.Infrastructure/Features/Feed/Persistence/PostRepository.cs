using Microsoft.EntityFrameworkCore;
using BKM.Integration.Domain.Features.Feed.Entities;
using BKM.Integration.Domain.Features.Feed.Interfaces;
using BKM.Integration.Infrastructure.Persistence;

namespace BKM.Integration.Infrastructure.Features.Feed.Persistence;

public sealed class PostRepository(AppDbContext db) : IPostRepository
{
    public async Task<Post> SaveAsync(Post post, CancellationToken ct = default)
    {
        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);
        return post;
    }

    public Task<Post?> GetByIdAsync(long id, CancellationToken ct = default)
        => db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Post>> GetByUserAsync(
        string userId, int page, int pageSize, CancellationToken ct = default)
        => await db.Posts
               .AsNoTracking()
               .Where(p => p.UserId == userId)
               .OrderByDescending(p => p.CreatedAt)
               .Skip((page - 1) * pageSize)
               .Take(pageSize)
               .ToListAsync(ct);

    public Task<int> CountByUserAsync(string userId, CancellationToken ct = default)
        => db.Posts.CountAsync(p => p.UserId == userId, ct);
}

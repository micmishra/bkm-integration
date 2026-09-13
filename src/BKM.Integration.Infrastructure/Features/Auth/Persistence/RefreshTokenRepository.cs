using Microsoft.EntityFrameworkCore;
using BKM.Integration.Domain.Features.Auth.Entities;
using BKM.Integration.Domain.Features.Auth.Interfaces;
using BKM.Integration.Infrastructure.Persistence;

namespace BKM.Integration.Infrastructure.Features.Auth.Persistence;

/// <summary>
/// EF Core repository for refresh tokens.
/// </summary>
public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public async Task SaveAsync(RefreshToken token, CancellationToken ct = default)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => db.RefreshTokens
             .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task RevokeAsync(string tokenHash, CancellationToken ct = default)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (token is not null)
        {
            token.IsRevoked = true;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RevokeAllForUserAsync(string userId, CancellationToken ct = default)
    {
        await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), ct);
    }
}

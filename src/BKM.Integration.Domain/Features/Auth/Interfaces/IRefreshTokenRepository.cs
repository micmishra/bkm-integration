using BKM.Integration.Domain.Features.Auth.Entities;

namespace BKM.Integration.Domain.Features.Auth.Interfaces;

/// <summary>
/// Persistence contract for refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    Task SaveAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task RevokeAsync(string tokenHash, CancellationToken ct = default);
    Task RevokeAllForUserAsync(string userId, CancellationToken ct = default);
}

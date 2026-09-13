using BKM.Utility.Domain.Features.Auth.Entities;

namespace BKM.Utility.Domain.Features.Auth.Interfaces;

/// <summary>
/// Generates and validates JWT access tokens and opaque refresh tokens.
/// Implemented in Infrastructure (requires JWT package) but abstracted here in Domain.
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a signed JWT access token embedding user identity, roles, and permissions.</summary>
    string GenerateAccessToken(AppUser user, IList<string> roles, IList<string> permissions);

    /// <summary>Creates a cryptographically random 64-byte opaque refresh token (Base64-encoded).</summary>
    string GenerateRefreshToken();

    /// <summary>Computes the SHA-256 hex hash of <paramref name="token"/>.</summary>
    string HashToken(string token);
}

namespace BKM.Integration.Domain.Features.Auth.Entities;

/// <summary>
/// Persisted refresh token record.
/// TokenHash stores the SHA-256 hash of the raw token — never the raw token itself.
/// Stored in dbo.RefreshTokens.
/// </summary>
public sealed class RefreshToken
{
    public long     Id         { get; set; }
    public string   UserId     { get; set; } = string.Empty;
    public string   TokenHash  { get; set; } = string.Empty;  // SHA-256 of the token
    public DateTime ExpiresAt  { get; set; }
    public bool     IsRevoked  { get; set; }
    public DateTime CreatedAt  { get; set; }
    public string?  DeviceInfo { get; set; }
}

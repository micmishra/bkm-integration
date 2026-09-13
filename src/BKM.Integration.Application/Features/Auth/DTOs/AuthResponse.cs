namespace BKM.Integration.Application.Features.Auth.DTOs;

public sealed class AuthResponse
{
    public string   AccessToken  { get; init; } = "";
    public string   RefreshToken { get; init; } = "";
    public DateTime ExpiresAt    { get; init; }
    public string   UserId       { get; init; } = "";
    public string   DisplayName  { get; init; } = "";
    public string   Email        { get; init; } = "";
    public IList<string> Roles   { get; init; } = [];
}

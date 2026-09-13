using BKM.Integration.Application.Features.Auth.DTOs;

namespace BKM.Integration.Application.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
    Task<string> GetExternalLoginUrlAsync(string provider, string redirectUri, CancellationToken ct = default);
    Task<AuthResponse> ExternalLoginCallbackAsync(string provider, string code, string redirectUri, CancellationToken ct = default);
}

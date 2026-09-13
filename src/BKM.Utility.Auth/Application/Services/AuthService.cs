using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using BKM.Utility.Application.Features.Auth.DTOs;
using BKM.Utility.Application.Features.Auth.Interfaces;
using BKM.Utility.Domain.Features.Auth.Entities;
using BKM.Utility.Domain.Features.Auth.Interfaces;

namespace BKM.Utility.Application.Features.Auth.Services;

/// <summary>
/// Handles registration, login, token refresh, revoke, and external OAuth2 login flows.
/// </summary>
public sealed class AuthService(
    UserManager<AppUser>      userManager,
    ITokenService             tokenService,
    IRefreshTokenRepository   refreshTokenRepo,
    IConfiguration            configuration,
    IHttpClientFactory        httpClientFactory) : IAuthService
{
    // ── Register ───────────────────────────────────────────────────────────────

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var user = new AppUser
        {
            UserName    = request.Email,
            Email       = request.Email,
            DisplayName = request.DisplayName,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, "User");

        return await BuildAuthResponseAsync(user, ct);
    }

    // ── Login ──────────────────────────────────────────────────────────────────

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return await BuildAuthResponseAsync(user, ct);
    }

    // ── Refresh ────────────────────────────────────────────────────────────────

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash    = tokenService.HashToken(refreshToken);
        var stored  = await refreshTokenRepo.GetByHashAsync(hash, ct)
            ?? throw new UnauthorizedAccessException("Refresh token not found.");

        if (stored.IsRevoked)
            throw new UnauthorizedAccessException("Refresh token has been revoked.");

        if (stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        await refreshTokenRepo.RevokeAsync(hash, ct);

        var user = await userManager.FindByIdAsync(stored.UserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        return await BuildAuthResponseAsync(user, ct);
    }

    // ── Revoke ─────────────────────────────────────────────────────────────────

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = tokenService.HashToken(refreshToken);
        await refreshTokenRepo.RevokeAsync(hash, ct);
    }

    // ── External SSO ──────────────────────────────────────────────────────────

    public Task<string> GetExternalLoginUrlAsync(string provider, string redirectUri, CancellationToken ct = default)
    {
        var url = provider.ToLowerInvariant() switch
        {
            "google" => BuildGoogleAuthUrl(redirectUri),
            "microsoft" => BuildMicrosoftAuthUrl(redirectUri),
            _ => throw new NotSupportedException($"SSO provider '{provider}' is not supported.")
        };
        return Task.FromResult(url);
    }

    public async Task<AuthResponse> ExternalLoginCallbackAsync(
        string provider, string code, string redirectUri, CancellationToken ct = default)
    {
        var (externalUserId, email, displayName) = provider.ToLowerInvariant() switch
        {
            "google"    => await ExchangeGoogleCodeAsync(code, redirectUri, ct),
            "microsoft" => await ExchangeMicrosoftCodeAsync(code, redirectUri, ct),
            _ => throw new NotSupportedException($"SSO provider '{provider}' is not supported.")
        };

        // Find or create local user
        var user = await userManager.FindByLoginAsync(provider, externalUserId);
        if (user is null)
        {
            user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser
                {
                    UserName       = email,
                    Email          = email,
                    DisplayName    = displayName,
                    IsActive       = true,
                    CreatedAt      = DateTime.UtcNow,
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));

                await userManager.AddToRoleAsync(user, "User");
            }

            await userManager.AddLoginAsync(user, new UserLoginInfo(provider, externalUserId, provider));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return await BuildAuthResponseAsync(user, ct);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<AuthResponse> BuildAuthResponseAsync(AppUser user, CancellationToken ct)
    {
        var roles       = (await userManager.GetRolesAsync(user)).ToList();
        var claims      = await userManager.GetClaimsAsync(user);
        var permissions = claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();

        var refreshDays   = int.Parse(configuration["Jwt:RefreshTokenDays"] ?? "30");
        var accessMinutes = int.Parse(configuration["Jwt:AccessTokenMinutes"] ?? "60");
        var expiresAt     = DateTime.UtcNow.AddMinutes(accessMinutes);

        var accessToken  = tokenService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = tokenService.GenerateRefreshToken();
        var tokenHash    = tokenService.HashToken(refreshToken);

        await refreshTokenRepo.SaveAsync(new RefreshToken
        {
            UserId    = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        }, ct);

        return new AuthResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt    = expiresAt,
            UserId       = user.Id,
            DisplayName  = user.DisplayName,
            Email        = user.Email ?? "",
            Roles        = roles
        };
    }

    private string BuildGoogleAuthUrl(string redirectUri)
    {
        var clientId = configuration["Sso:Google:ClientId"] ?? "";
        var scope    = Uri.EscapeDataString("openid email profile");
        var redirect = Uri.EscapeDataString(redirectUri);
        return $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirect}&response_type=code&scope={scope}";
    }

    private string BuildMicrosoftAuthUrl(string redirectUri)
    {
        var clientId = configuration["Sso:Microsoft:ClientId"] ?? "";
        var tenantId = configuration["Sso:Microsoft:TenantId"] ?? "common";
        var scope    = Uri.EscapeDataString("openid email profile");
        var redirect = Uri.EscapeDataString(redirectUri);
        return $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize?client_id={clientId}&redirect_uri={redirect}&response_type=code&scope={scope}";
    }

    private async Task<(string UserId, string Email, string DisplayName)> ExchangeGoogleCodeAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        var clientId     = configuration["Sso:Google:ClientId"] ?? "";
        var clientSecret = configuration["Sso:Google:ClientSecret"] ?? "";

        var client = httpClientFactory.CreateClient();
        var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"]          = code,
                ["client_id"]     = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"]  = redirectUri,
                ["grant_type"]    = "authorization_code"
            }), ct);

        tokenResponse.EnsureSuccessStatusCode();
        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to parse Google token response.");

        var userInfoResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "https://www.googleapis.com/oauth2/v3/userinfo")
        {
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken) }
        }, ct);

        userInfoResponse.EnsureSuccessStatusCode();
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to parse Google userinfo response.");

        return (userInfo.Sub, userInfo.Email, userInfo.Name ?? userInfo.Email);
    }

    private async Task<(string UserId, string Email, string DisplayName)> ExchangeMicrosoftCodeAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        var clientId     = configuration["Sso:Microsoft:ClientId"] ?? "";
        var clientSecret = configuration["Sso:Microsoft:ClientSecret"] ?? "";
        var tenantId     = configuration["Sso:Microsoft:TenantId"] ?? "common";

        var client = httpClientFactory.CreateClient();
        var tokenResponse = await client.PostAsync(
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"]          = code,
                ["client_id"]     = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"]  = redirectUri,
                ["grant_type"]    = "authorization_code",
                ["scope"]         = "openid email profile"
            }), ct);

        tokenResponse.EnsureSuccessStatusCode();
        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to parse Microsoft token response.");

        var userInfoResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "https://graph.microsoft.com/v1.0/me")
        {
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken) }
        }, ct);

        userInfoResponse.EnsureSuccessStatusCode();
        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<MicrosoftUserInfo>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to parse Microsoft userinfo response.");

        return (userInfo.Id, userInfo.Mail ?? userInfo.UserPrincipalName, userInfo.DisplayName ?? userInfo.UserPrincipalName);
    }

    // ── Private DTO records for OAuth responses ───────────────────────────────

    private sealed record GoogleTokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken);

    private sealed record GoogleUserInfo(
        [property: System.Text.Json.Serialization.JsonPropertyName("sub")]   string Sub,
        [property: System.Text.Json.Serialization.JsonPropertyName("email")] string Email,
        [property: System.Text.Json.Serialization.JsonPropertyName("name")]  string? Name);

    private sealed record MicrosoftTokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken);

    private sealed record MicrosoftUserInfo(
        [property: System.Text.Json.Serialization.JsonPropertyName("id")]                string Id,
        [property: System.Text.Json.Serialization.JsonPropertyName("mail")]              string? Mail,
        [property: System.Text.Json.Serialization.JsonPropertyName("userPrincipalName")] string UserPrincipalName,
        [property: System.Text.Json.Serialization.JsonPropertyName("displayName")]       string? DisplayName);
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.Auth.DTOs;
using BKM.Utility.Application.Features.Auth.Interfaces;

namespace BKM.Utility.Api.Features.Auth;

/// <summary>
/// Authentication endpoints — registration, login, token refresh/revoke, and SSO.
///
///   POST /api/auth/register          — register a new user
///   POST /api/auth/login             — password login → JWT + refresh token
///   POST /api/auth/refresh           — exchange refresh token for new token pair
///   POST /api/auth/revoke            — revoke a refresh token (requires auth)
///   GET  /api/auth/me                — current user info from JWT claims (requires auth)
///   GET  /api/auth/external          — get SSO authorization URL
///   POST /api/auth/external/callback — exchange OAuth2 code for JWT
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return StatusCode(201, ApiResponseBuilder.Ok(result, "User registered successfully."));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return Ok(ApiResponseBuilder.Ok(result, "Login successful."));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(request.RefreshToken, ct);
        return Ok(ApiResponseBuilder.Ok(result, "Token refreshed successfully."));
    }

    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await authService.RevokeAsync(request.RefreshToken, ct);
        return Ok(ApiResponseBuilder.Ok<object?>(null, "Refresh token revoked."));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(401)]
    public IActionResult Me()
    {
        var principal = User;
        var dto = new UserDto
        {
            Id          = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? principal.FindFirstValue("sub") ?? "",
            Email       = principal.FindFirstValue(ClaimTypes.Email)
                       ?? principal.FindFirstValue("email") ?? "",
            DisplayName = principal.FindFirstValue("name") ?? "",
            IsActive    = true,
            Roles       = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        };
        return Ok(ApiResponseBuilder.Ok(dto, "Current user info."));
    }

    [HttpGet("external")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetExternalLoginUrl(
        [FromQuery] string provider,
        [FromQuery] string redirectUri,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(ApiResponseBuilder.Error(ErrorCodes.BadRequest, "provider and redirectUri are required."));

        var url = await authService.GetExternalLoginUrlAsync(provider, redirectUri, ct);
        return Ok(ApiResponseBuilder.Ok(url, "Authorization URL generated."));
    }

    [HttpPost("external/callback")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ExternalCallback(
        [FromQuery] string provider,
        [FromQuery] string code,
        [FromQuery] string redirectUri,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(ApiResponseBuilder.Error(ErrorCodes.BadRequest, "provider, code and redirectUri are required."));

        var result = await authService.ExternalLoginCallbackAsync(provider, code, redirectUri, ct);
        return Ok(ApiResponseBuilder.Ok(result, "External login successful."));
    }
}

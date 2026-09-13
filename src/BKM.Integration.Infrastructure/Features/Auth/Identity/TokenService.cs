using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using BKM.Integration.Domain.Features.Auth.Entities;
using BKM.Integration.Domain.Features.Auth.Interfaces;

namespace BKM.Integration.Infrastructure.Features.Auth.Identity;

/// <summary>
/// JWT access token generator and refresh token utilities.
/// Implements the domain ITokenService contract.
/// </summary>
public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    public string GenerateAccessToken(AppUser user, IList<string> roles, IList<string> permissions)
    {
        var secretKey     = configuration["Jwt:SecretKey"]!;
        var issuer        = configuration["Jwt:Issuer"]!;
        var audience      = configuration["Jwt:Audience"]!;
        var minutesConfig = configuration["Jwt:AccessTokenMinutes"];
        var minutes       = int.TryParse(minutesConfig, out var m) ? m : 60;

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new("name",                         user.DisplayName),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(r       => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var token = new JwtSecurityToken(
            issuer:             issuer,
            audience:           audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

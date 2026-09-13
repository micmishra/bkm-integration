using System.ComponentModel.DataAnnotations;

namespace BKM.Integration.Application.Features.Auth.DTOs;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = "";
}

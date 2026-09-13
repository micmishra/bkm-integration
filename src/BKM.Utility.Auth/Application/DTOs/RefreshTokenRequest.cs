using System.ComponentModel.DataAnnotations;

namespace BKM.Utility.Application.Features.Auth.DTOs;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = "";
}

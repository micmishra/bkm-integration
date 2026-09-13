using System.ComponentModel.DataAnnotations;

namespace BKM.Utility.Application.Features.Auth.DTOs;

public sealed class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = "";
}

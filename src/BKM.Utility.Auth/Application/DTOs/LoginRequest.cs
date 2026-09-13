using System.ComponentModel.DataAnnotations;

namespace BKM.Utility.Application.Features.Auth.DTOs;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

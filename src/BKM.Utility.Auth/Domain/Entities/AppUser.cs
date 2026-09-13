using Microsoft.AspNetCore.Identity;

namespace BKM.Utility.Domain.Features.Auth.Entities;

/// <summary>
/// Extended ASP.NET Core Identity user with BKM-specific fields.
/// Stored in dbo.AspNetUsers (Identity default).
/// </summary>
public sealed class AppUser : IdentityUser
{
    public string    DisplayName { get; set; } = string.Empty;
    public bool      IsActive    { get; set; } = true;
    public DateTime  CreatedAt   { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

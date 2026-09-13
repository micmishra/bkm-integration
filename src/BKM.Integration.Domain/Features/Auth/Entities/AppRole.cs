using Microsoft.AspNetCore.Identity;

namespace BKM.Integration.Domain.Features.Auth.Entities;

/// <summary>
/// Extended ASP.NET Core Identity role with BKM-specific fields.
/// Stored in dbo.AspNetRoles (Identity default).
/// </summary>
public sealed class AppRole : IdentityRole
{
    public string?  Description { get; set; }
    public DateTime CreatedAt   { get; set; }
}

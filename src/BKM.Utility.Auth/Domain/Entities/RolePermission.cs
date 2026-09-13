namespace BKM.Utility.Domain.Features.Auth.Entities;

/// <summary>
/// Maps a role to a permission (many-to-many join).
/// Stored in dbo.RolePermissions.
/// </summary>
public sealed class RolePermission
{
    public int    Id           { get; set; }
    public string RoleId       { get; set; } = string.Empty;
    public int    PermissionId { get; set; }
}

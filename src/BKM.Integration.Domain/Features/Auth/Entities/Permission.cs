namespace BKM.Integration.Domain.Features.Auth.Entities;

/// <summary>
/// A named, fine-grained permission (e.g. "posts:create", "users:manage").
/// Stored in dbo.Permissions.
/// </summary>
public sealed class Permission
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;  // e.g. "posts:create"
    public string  Resource    { get; set; } = string.Empty;  // e.g. "posts"
    public string  Action      { get; set; } = string.Empty;  // e.g. "create"
    public string? Description { get; set; }
}

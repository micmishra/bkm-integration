namespace BKM.Utility.Application.Features.Auth.DTOs;

public sealed class RoleDto
{
    public string   Id          { get; init; } = "";
    public string   Name        { get; init; } = "";
    public string?  Description { get; init; }
    public DateTime CreatedAt   { get; init; }
    public IList<string> Permissions { get; init; } = [];
}

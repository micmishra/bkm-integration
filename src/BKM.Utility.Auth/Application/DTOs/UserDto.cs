namespace BKM.Utility.Application.Features.Auth.DTOs;

public sealed class UserDto
{
    public string    Id          { get; init; } = "";
    public string    Email       { get; init; } = "";
    public string    DisplayName { get; init; } = "";
    public bool      IsActive    { get; init; }
    public DateTime  CreatedAt   { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public IList<string> Roles   { get; init; } = [];
}

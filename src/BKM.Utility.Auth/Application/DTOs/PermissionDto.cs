namespace BKM.Utility.Application.Features.Auth.DTOs;

public sealed class PermissionDto
{
    public int     Id          { get; init; }
    public string  Name        { get; init; } = "";
    public string  Resource    { get; init; } = "";
    public string  Action      { get; init; } = "";
    public string? Description { get; init; }
}

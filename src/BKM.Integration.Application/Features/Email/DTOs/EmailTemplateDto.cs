namespace BKM.Integration.Application.Features.Email.DTOs;

public sealed class EmailTemplateDto
{
    public int      Id          { get; init; }
    public string   Name        { get; init; } = string.Empty;
    public string   Subject     { get; init; } = string.Empty;
    public string   Body        { get; init; } = string.Empty;
    public bool     IsHtml      { get; init; }
    public bool     IsActive    { get; init; }
    public string?  Description { get; init; }
    public DateTime CreatedAt   { get; init; }
    public DateTime UpdatedAt   { get; init; }
}

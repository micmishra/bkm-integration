namespace BKM.Utility.Application.Features.Email.DTOs;

public sealed class EmailAuditLogDto
{
    public long     Id           { get; init; }
    public string   ToAddress    { get; init; } = string.Empty;
    public string   Subject      { get; init; } = string.Empty;
    public string?  TemplateName { get; init; }
    public bool     Success      { get; init; }
    public string?  ErrorMessage { get; init; }
    public long     DurationMs   { get; init; }
    public DateTime SentAt       { get; init; }
}

public sealed class EmailAuditQueryRequest
{
    public string?   ToAddress { get; set; }
    public bool?     Success   { get; set; }
    public DateTime? From      { get; set; }
    public DateTime? To        { get; set; }
    public int       Page      { get; set; } = 1;
    public int       PageSize  { get; set; } = 50;
}

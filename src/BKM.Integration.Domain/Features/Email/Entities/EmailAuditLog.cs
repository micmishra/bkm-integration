namespace BKM.Integration.Domain.Features.Email.Entities;

/// <summary>
/// Immutable audit record written for every send attempt (success or failure).
/// Stored in dbo.EmailAuditLogs.
/// </summary>
public sealed class EmailAuditLog
{
    public long    Id            { get; set; }
    public string  ToAddress     { get; set; } = string.Empty;
    public string  Subject       { get; set; } = string.Empty;

    /// <summary>Template name used, or null for raw sends.</summary>
    public string? TemplateName  { get; set; }

    public bool    Success       { get; set; }

    /// <summary>Error message on failure; null on success.</summary>
    public string? ErrorMessage  { get; set; }

    /// <summary>Duration of the send attempt in milliseconds.</summary>
    public long    DurationMs    { get; set; }

    public DateTime SentAt       { get; set; }
}

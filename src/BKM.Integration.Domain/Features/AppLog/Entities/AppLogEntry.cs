namespace BKM.Integration.Domain.Features.AppLog.Entities;

/// <summary>
/// Represents a structured application log entry persisted to SQL Server.
/// Maps to [dbo].[AppLogs] table via EF Core Code First.
/// </summary>
public sealed class AppLogEntry
{
    public long      Id          { get; set; }
    public string    Level       { get; set; } = string.Empty;   // Verbose|Debug|Information|Warning|Error|Fatal
    public string    Message     { get; set; } = string.Empty;
    public string?   Exception   { get; set; }
    public string?   TraceId     { get; set; }
    public string?   Feature     { get; set; }                   // e.g. "UrlShortener", "Encryption"
    public string?   MachineName { get; set; }
    public string?   Environment { get; set; }                   // Development|Staging|Production
    public string?   Properties  { get; set; }                   // JSON bag of all Serilog properties
    public DateTime  Timestamp   { get; set; } = DateTime.UtcNow;
}

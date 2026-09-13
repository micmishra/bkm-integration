namespace BKM.Utility.Application.Features.AppLog.DTOs;

public sealed class AppLogEntryDto
{
    public long     Id          { get; init; }
    public string   Level       { get; init; } = string.Empty;
    public string   Message     { get; init; } = string.Empty;
    public string?  Exception   { get; init; }
    public string?  TraceId     { get; init; }
    public string?  Feature     { get; init; }
    public string?  MachineName { get; init; }
    public string?  Environment { get; init; }
    public string?  Properties  { get; init; }
    public DateTime Timestamp   { get; init; }
}

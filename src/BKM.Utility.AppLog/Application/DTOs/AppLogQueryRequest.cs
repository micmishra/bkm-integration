namespace BKM.Utility.Application.Features.AppLog.DTOs;

public sealed class AppLogQueryRequest
{
    /// <summary>Filter by level: Verbose, Debug, Information, Warning, Error, Fatal</summary>
    public string?   Level    { get; set; }

    /// <summary>Filter by feature name e.g. "UrlShortener", "Encryption"</summary>
    public string?   Feature  { get; set; }

    /// <summary>UTC start of time range (inclusive)</summary>
    public DateTime? From     { get; set; }

    /// <summary>UTC end of time range (inclusive)</summary>
    public DateTime? To       { get; set; }

    /// <summary>1-based page number. Default: 1</summary>
    public int       Page     { get; set; } = 1;

    /// <summary>Page size (1–200). Default: 50</summary>
    public int       PageSize { get; set; } = 50;
}

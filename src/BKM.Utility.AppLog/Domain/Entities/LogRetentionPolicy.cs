namespace BKM.Utility.Domain.Features.AppLog.Entities;

/// <summary>
/// Defines how long logs should be retained for a specific feature (DB + file).
/// Stored in [dbo].[LogRetentionPolicies] — managed entirely via API, no appsettings needed.
///
/// How matching works:
///   - Feature = "UrlShortener"  → applies only to UrlShortener logs
///   - Feature = "Encryption"    → applies only to Encryption logs
///   - Feature = null (default)  → fallback for any feature with no specific policy
///
/// RetentionDays = 0 means "keep forever" (never purge).
/// </summary>
public sealed class LogRetentionPolicy
{
    public int      Id                     { get; set; }

    /// <summary>
    /// Feature name this policy applies to.
    /// NULL = default policy (applies to all features not explicitly listed).
    /// </summary>
    public string?  Feature                { get; set; }

    /// <summary>Days to keep DB log entries. 0 = forever.</summary>
    public int      DbRetentionDays        { get; set; } = 30;

    /// <summary>Days to keep rolled log files on disk. 0 = forever.</summary>
    public int      FileRetentionDays      { get; set; } = 30;

    /// <summary>Human-readable description of why this policy exists.</summary>
    public string?  Description            { get; set; }

    /// <summary>UTC timestamp when this policy was last updated.</summary>
    public DateTime LastUpdatedAt          { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of last purge run for this policy.</summary>
    public DateTime? LastPurgedAt          { get; set; }

    /// <summary>Number of DB rows deleted in the last purge run.</summary>
    public long      LastPurgeDeletedCount { get; set; }
}

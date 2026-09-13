using System.ComponentModel.DataAnnotations;

namespace BKM.Integration.Application.Features.AppLog.DTOs;

public sealed class UpsertRetentionPolicyRequest
{
    /// <summary>
    /// Feature name this policy targets.
    /// Leave null or omit to update the default policy (applies to all unmatched features).
    /// </summary>
    public string? Feature { get; set; }

    /// <summary>Days to retain DB log entries for this feature. 0 = keep forever.</summary>
    [Range(0, 3650)]
    public int DbRetentionDays { get; set; } = 30;

    /// <summary>Days to retain rolled log files for this feature. 0 = keep forever.</summary>
    [Range(0, 3650)]
    public int FileRetentionDays { get; set; } = 30;

    /// <summary>Optional human-readable explanation.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}

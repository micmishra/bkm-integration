using BKM.Utility.Domain.Features.AppLog.Entities;

namespace BKM.Utility.Domain.Features.AppLog.Interfaces;

/// <summary>
/// Contract for querying persisted application log entries from the DB sink.
/// Write path is handled entirely by Serilog's custom DB sink — this is read-only.
/// </summary>
public interface IAppLogRepository
{
    /// <summary>Query recent log entries with optional filters.</summary>
    Task<IReadOnlyList<AppLogEntry>> QueryAsync(
        string?  level       = null,
        string?  feature     = null,
        DateTime? from       = null,
        DateTime? to         = null,
        int      page        = 1,
        int      pageSize    = 50,
        CancellationToken ct = default);

    /// <summary>Total count matching the same filters (for pagination meta).</summary>
    Task<int> CountAsync(
        string?  level   = null,
        string?  feature = null,
        DateTime? from   = null,
        DateTime? to     = null,
        CancellationToken ct = default);
}

namespace BKM.Integration.Application.Features.AppLog.DTOs;

/// <summary>
/// Cacheable page result for AppLog queries.
/// Stored in the distributed cache keyed by query fingerprint for up to 30 seconds.
/// </summary>
public sealed class AppLogPageResult
{
    public IReadOnlyList<AppLogEntryDto> Items      { get; init; } = [];
    public int                          TotalCount  { get; init; }
}

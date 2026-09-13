using BKM.Integration.Domain.Features.AppLog.Entities;
using BKM.Integration.Domain.Features.AppLog.Interfaces;
using BKM.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BKM.Integration.Infrastructure.Features.AppLog.Logging;

public sealed class AppLogRepository(AppDbContext db) : IAppLogRepository
{
    public async Task<IReadOnlyList<AppLogEntry>> QueryAsync(
        string?   level    = null,
        string?   feature  = null,
        DateTime? from     = null,
        DateTime? to       = null,
        int       page     = 1,
        int       pageSize = 50,
        CancellationToken ct = default)
    {
        var query = BuildQuery(level, feature, from, to);

        return await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public Task<int> CountAsync(
        string?   level   = null,
        string?   feature = null,
        DateTime? from    = null,
        DateTime? to      = null,
        CancellationToken ct = default)
        => BuildQuery(level, feature, from, to).CountAsync(ct);

    private IQueryable<AppLogEntry> BuildQuery(
        string? level, string? feature, DateTime? from, DateTime? to)
    {
        var q = db.AppLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(level))
            q = q.Where(x => x.Level == level);

        if (!string.IsNullOrWhiteSpace(feature))
            q = q.Where(x => x.Feature == feature);

        if (from.HasValue)
            q = q.Where(x => x.Timestamp >= from.Value);

        if (to.HasValue)
            q = q.Where(x => x.Timestamp <= to.Value);

        return q;
    }
}

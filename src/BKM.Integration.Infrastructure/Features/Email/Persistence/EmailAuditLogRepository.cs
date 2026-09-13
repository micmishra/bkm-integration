using BKM.Integration.Domain.Features.Email.Entities;
using BKM.Integration.Domain.Features.Email.Interfaces;
using BKM.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BKM.Integration.Infrastructure.Features.Email.Persistence;

public sealed class EmailAuditLogRepository(AppDbContext db) : IEmailAuditLogRepository
{
    public async Task WriteAsync(EmailAuditLog entry, CancellationToken ct = default)
    {
        db.EmailAuditLogs.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<EmailAuditLog>> QueryAsync(
        string? toAddress, bool? success,
        DateTime? from, DateTime? to,
        int page, int pageSize,
        CancellationToken ct = default)
    {
        var q = db.EmailAuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(toAddress))
            q = q.Where(x => x.ToAddress.Contains(toAddress));

        if (success.HasValue)
            q = q.Where(x => x.Success == success.Value);

        if (from.HasValue)
            q = q.Where(x => x.SentAt >= from.Value);

        if (to.HasValue)
            q = q.Where(x => x.SentAt <= to.Value);

        return q.OrderByDescending(x => x.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct)
                .ContinueWith<IReadOnlyList<EmailAuditLog>>(t => t.Result, ct);
    }

    public Task<int> CountAsync(
        string? toAddress, bool? success,
        DateTime? from, DateTime? to,
        CancellationToken ct = default)
    {
        var q = db.EmailAuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(toAddress))
            q = q.Where(x => x.ToAddress.Contains(toAddress));

        if (success.HasValue)
            q = q.Where(x => x.Success == success.Value);

        if (from.HasValue)
            q = q.Where(x => x.SentAt >= from.Value);

        if (to.HasValue)
            q = q.Where(x => x.SentAt <= to.Value);

        return q.CountAsync(ct);
    }
}

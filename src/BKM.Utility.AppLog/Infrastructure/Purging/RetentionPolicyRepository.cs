using BKM.Utility.Domain.Features.AppLog.Entities;
using BKM.Utility.Domain.Features.AppLog.Interfaces;
using BKM.Utility.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BKM.Utility.Infrastructure.Features.AppLog.Purging;

public sealed class RetentionPolicyRepository(AppLogDbContext db) : IRetentionPolicyRepository
{
    public Task<IReadOnlyList<LogRetentionPolicy>> GetAllAsync(CancellationToken ct = default)
        => db.LogRetentionPolicies
             .AsNoTracking()
             .OrderBy(x => x.Feature == null ? 1 : 0)   // default (null) last
             .ThenBy(x => x.Feature)
             .ToListAsync(ct)
             .ContinueWith<IReadOnlyList<LogRetentionPolicy>>(t => t.Result, ct);

    public Task<LogRetentionPolicy?> GetByFeatureAsync(string? feature, CancellationToken ct = default)
        => feature is null
            ? db.LogRetentionPolicies.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Feature == null, ct)
            : db.LogRetentionPolicies.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Feature == feature, ct);

    public async Task<LogRetentionPolicy?> GetEffectiveAsync(string feature, CancellationToken ct = default)
    {
        // Feature-specific policy first
        var specific = await db.LogRetentionPolicies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Feature == feature, ct);

        if (specific is not null) return specific;

        // Fall back to default policy
        return await db.LogRetentionPolicies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Feature == null, ct);
    }

    public async Task UpsertAsync(LogRetentionPolicy policy, CancellationToken ct = default)
    {
        var existing = policy.Feature is null
            ? await db.LogRetentionPolicies.FirstOrDefaultAsync(x => x.Feature == null, ct)
            : await db.LogRetentionPolicies.FirstOrDefaultAsync(x => x.Feature == policy.Feature, ct);

        if (existing is null)
        {
            db.LogRetentionPolicies.Add(policy);
        }
        else
        {
            existing.DbRetentionDays   = policy.DbRetentionDays;
            existing.FileRetentionDays = policy.FileRetentionDays;
            existing.Description       = policy.Description;
            existing.LastUpdatedAt     = policy.LastUpdatedAt;
            db.LogRetentionPolicies.Update(existing);
            // return id on original policy for mapping
            policy.Id = existing.Id;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task UpdatePurgeStatsAsync(
        int id, DateTime purgedAt, long deletedCount, CancellationToken ct = default)
    {
        var policy = await db.LogRetentionPolicies.FindAsync([id], ct);
        if (policy is null) return;
        policy.LastPurgedAt          = purgedAt;
        policy.LastPurgeDeletedCount = deletedCount;
        await db.SaveChangesAsync(ct);
    }
}

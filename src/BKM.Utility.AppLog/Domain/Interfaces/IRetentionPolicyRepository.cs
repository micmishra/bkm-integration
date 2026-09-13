using BKM.Utility.Domain.Features.AppLog.Entities;

namespace BKM.Utility.Domain.Features.AppLog.Interfaces;

/// <summary>Read/write access to log retention policies stored in the DB.</summary>
public interface IRetentionPolicyRepository
{
    /// <summary>Returns all policies ordered by Feature (nulls last = default policy).</summary>
    Task<IReadOnlyList<LogRetentionPolicy>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns the policy for a specific feature, or null if none defined.</summary>
    Task<LogRetentionPolicy?> GetByFeatureAsync(string? feature, CancellationToken ct = default);

    /// <summary>
    /// Returns the effective policy for a feature:
    /// feature-specific policy if it exists, otherwise the default (Feature = null) policy.
    /// </summary>
    Task<LogRetentionPolicy?> GetEffectiveAsync(string feature, CancellationToken ct = default);

    Task UpsertAsync(LogRetentionPolicy policy, CancellationToken ct = default);

    Task UpdatePurgeStatsAsync(int id, DateTime purgedAt, long deletedCount, CancellationToken ct = default);
}

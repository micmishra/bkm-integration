using BKM.Integration.Application.Features.AppLog.DTOs;
using BKM.Integration.Application.Features.AppLog.Interfaces;
using BKM.Integration.Domain.Features.AppLog.Entities;
using BKM.Integration.Domain.Features.AppLog.Interfaces;

namespace BKM.Integration.Application.Features.AppLog.Services;

public sealed class RetentionPolicyService(IRetentionPolicyRepository repository) : IRetentionPolicyService
{
    public async Task<IReadOnlyList<RetentionPolicyDto>> GetAllAsync(CancellationToken ct = default)
    {
        var policies = await repository.GetAllAsync(ct);
        return policies.Select(Map).ToList();
    }

    public async Task<RetentionPolicyDto> UpsertAsync(
        UpsertRetentionPolicyRequest request,
        CancellationToken ct = default)
    {
        var policy = new LogRetentionPolicy
        {
            Feature           = string.IsNullOrWhiteSpace(request.Feature) ? null : request.Feature.Trim(),
            DbRetentionDays   = request.DbRetentionDays,
            FileRetentionDays = request.FileRetentionDays,
            Description       = request.Description,
            LastUpdatedAt     = DateTime.UtcNow
        };

        await repository.UpsertAsync(policy, ct);
        return Map(policy);
    }

    private static RetentionPolicyDto Map(LogRetentionPolicy p) => new()
    {
        Id                    = p.Id,
        Feature               = p.Feature,
        DbRetentionDays       = p.DbRetentionDays,
        FileRetentionDays     = p.FileRetentionDays,
        Description           = p.Description,
        LastUpdatedAt         = p.LastUpdatedAt,
        LastPurgedAt          = p.LastPurgedAt,
        LastPurgeDeletedCount = p.LastPurgeDeletedCount
    };
}

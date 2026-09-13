using BKM.Utility.Application.Features.AppLog.DTOs;

namespace BKM.Utility.Application.Features.AppLog.Interfaces;

public interface IRetentionPolicyService
{
    Task<IReadOnlyList<RetentionPolicyDto>> GetAllAsync(CancellationToken ct = default);
    Task<RetentionPolicyDto> UpsertAsync(UpsertRetentionPolicyRequest request, CancellationToken ct = default);
}

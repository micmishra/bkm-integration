using BKM.Integration.Application.Features.AppLog.DTOs;

namespace BKM.Integration.Application.Features.AppLog.Interfaces;

public interface IRetentionPolicyService
{
    Task<IReadOnlyList<RetentionPolicyDto>> GetAllAsync(CancellationToken ct = default);
    Task<RetentionPolicyDto> UpsertAsync(UpsertRetentionPolicyRequest request, CancellationToken ct = default);
}

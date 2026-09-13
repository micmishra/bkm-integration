using BKM.Integration.Application.Features.Auth.DTOs;

namespace BKM.Integration.Application.Features.Auth.Interfaces;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken ct = default);
    Task<PermissionDto> UpsertAsync(UpsertPermissionRequest request, int? id = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

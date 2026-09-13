using BKM.Integration.Domain.Features.Auth.Entities;

namespace BKM.Integration.Domain.Features.Auth.Interfaces;

/// <summary>
/// Persistence contract for permissions and role-permission assignments.
/// </summary>
public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default);
    Task<Permission?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Permission>> GetForRoleAsync(string roleId, CancellationToken ct = default);
    Task<Permission> SaveAsync(Permission permission, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task AssignToRoleAsync(string roleId, int permissionId, CancellationToken ct = default);
    Task RemoveFromRoleAsync(string roleId, int permissionId, CancellationToken ct = default);
}

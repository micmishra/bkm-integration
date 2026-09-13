using BKM.Utility.Application.Features.Auth.DTOs;

namespace BKM.Utility.Application.Features.Auth.Interfaces;

public interface IRoleManagementService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);
    Task<RoleDto> CreateRoleAsync(string name, string? description, CancellationToken ct = default);
    Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default);
    Task<RoleDto> AssignPermissionAsync(string roleId, int permissionId, CancellationToken ct = default);
    Task<RoleDto> RemovePermissionAsync(string roleId, int permissionId, CancellationToken ct = default);
}

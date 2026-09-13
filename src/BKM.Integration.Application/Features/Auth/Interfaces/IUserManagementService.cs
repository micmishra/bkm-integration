using BKM.Integration.Application.Features.Auth.DTOs;

namespace BKM.Integration.Application.Features.Auth.Interfaces;

public interface IUserManagementService
{
    Task<(IReadOnlyList<UserDto> Items, int Total)> GetUsersAsync(int page, int pageSize, CancellationToken ct = default);
    Task<UserDto> GetUserByIdAsync(string userId, CancellationToken ct = default);
    Task<UserDto> AssignRoleAsync(string userId, AssignRoleRequest request, CancellationToken ct = default);
    Task<UserDto> RemoveRoleAsync(string userId, string roleName, CancellationToken ct = default);
    Task<bool> DeactivateUserAsync(string userId, CancellationToken ct = default);
    Task<bool> ActivateUserAsync(string userId, CancellationToken ct = default);
}

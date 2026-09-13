using Microsoft.AspNetCore.Identity;
using BKM.Utility.Application.Features.Auth.DTOs;
using BKM.Utility.Application.Features.Auth.Interfaces;
using BKM.Utility.Domain.Features.Auth.Entities;
using BKM.Utility.Domain.Features.Auth.Interfaces;

namespace BKM.Utility.Application.Features.Auth.Services;

/// <summary>
/// Admin service for managing roles and their permission assignments.
/// </summary>
public sealed class RoleManagementService(
    RoleManager<AppRole>  roleManager,
    IPermissionRepository permissionRepo) : IRoleManagementService
{
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        var roles = roleManager.Roles.OrderBy(r => r.Name).ToList();
        var dtos  = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            var perms = await permissionRepo.GetForRoleAsync(role.Id, ct);
            dtos.Add(ToDto(role, perms.Select(p => p.Name).ToList()));
        }
        return dtos.AsReadOnly();
    }

    public async Task<RoleDto> CreateRoleAsync(
        string name, string? description, CancellationToken ct = default)
    {
        var existing = await roleManager.FindByNameAsync(name);
        if (existing is not null)
            throw new InvalidOperationException($"Role '{name}' already exists.");

        var role = new AppRole { Name = name, Description = description, CreatedAt = DateTime.UtcNow };
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));

        return ToDto(role, []);
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return false;
        var result = await roleManager.DeleteAsync(role);
        return result.Succeeded;
    }

    public async Task<RoleDto> AssignPermissionAsync(
        string roleId, int permissionId, CancellationToken ct = default)
    {
        var role = await roleManager.FindByIdAsync(roleId)
            ?? throw new KeyNotFoundException($"Role '{roleId}' not found.");

        await permissionRepo.AssignToRoleAsync(roleId, permissionId, ct);

        var perms = await permissionRepo.GetForRoleAsync(roleId, ct);
        return ToDto(role, perms.Select(p => p.Name).ToList());
    }

    public async Task<RoleDto> RemovePermissionAsync(
        string roleId, int permissionId, CancellationToken ct = default)
    {
        var role = await roleManager.FindByIdAsync(roleId)
            ?? throw new KeyNotFoundException($"Role '{roleId}' not found.");

        await permissionRepo.RemoveFromRoleAsync(roleId, permissionId, ct);

        var perms = await permissionRepo.GetForRoleAsync(roleId, ct);
        return ToDto(role, perms.Select(p => p.Name).ToList());
    }

    private static RoleDto ToDto(AppRole role, IList<string> permissions) => new()
    {
        Id          = role.Id,
        Name        = role.Name ?? "",
        Description = role.Description,
        CreatedAt   = role.CreatedAt,
        Permissions = permissions
    };
}

using BKM.Integration.Application.Features.Auth.DTOs;
using BKM.Integration.Application.Features.Auth.Interfaces;
using BKM.Integration.Domain.Features.Auth.Entities;
using BKM.Integration.Domain.Features.Auth.Interfaces;

namespace BKM.Integration.Application.Features.Auth.Services;

/// <summary>
/// CRUD service for named permissions.
/// </summary>
public sealed class PermissionService(IPermissionRepository permissionRepo) : IPermissionService
{
    public async Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await permissionRepo.GetAllAsync(ct);
        return all.Select(ToDto).ToList().AsReadOnly();
    }

    public async Task<PermissionDto> UpsertAsync(
        UpsertPermissionRequest request, int? id = null, CancellationToken ct = default)
    {
        Permission permission;

        if (id.HasValue)
        {
            permission = await permissionRepo.GetByIdAsync(id.Value, ct)
                ?? throw new KeyNotFoundException($"Permission {id.Value} not found.");
            permission.Name        = request.Name;
            permission.Resource    = request.Resource;
            permission.Action      = request.Action;
            permission.Description = request.Description;
        }
        else
        {
            permission = new Permission
            {
                Name        = request.Name,
                Resource    = request.Resource,
                Action      = request.Action,
                Description = request.Description
            };
        }

        var saved = await permissionRepo.SaveAsync(permission, ct);
        return ToDto(saved);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        => permissionRepo.DeleteAsync(id, ct);

    private static PermissionDto ToDto(Permission p) => new()
    {
        Id          = p.Id,
        Name        = p.Name,
        Resource    = p.Resource,
        Action      = p.Action,
        Description = p.Description
    };
}

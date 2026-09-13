using Microsoft.EntityFrameworkCore;
using BKM.Utility.Domain.Features.Auth.Entities;
using BKM.Utility.Domain.Features.Auth.Interfaces;
using BKM.Utility.Infrastructure.Persistence;

namespace BKM.Utility.Infrastructure.Features.Auth.Persistence;

/// <summary>
/// EF Core repository for permissions and role-permission mappings.
/// </summary>
public sealed class PermissionRepository(AuthDbContext db) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default)
        => await db.Permissions.OrderBy(p => p.Name).ToListAsync(ct);

    public Task<Permission?> GetByIdAsync(int id, CancellationToken ct = default)
        => db.Permissions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Permission>> GetForRoleAsync(string roleId, CancellationToken ct = default)
        => await db.RolePermissions
             .Where(rp => rp.RoleId == roleId)
             .Join(db.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p)
             .OrderBy(p => p.Name)
             .ToListAsync(ct);

    public async Task<Permission> SaveAsync(Permission permission, CancellationToken ct = default)
    {
        if (permission.Id == 0)
            db.Permissions.Add(permission);
        else
            db.Permissions.Update(permission);

        await db.SaveChangesAsync(ct);
        return permission;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (permission is null) return false;

        db.Permissions.Remove(permission);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AssignToRoleAsync(string roleId, int permissionId, CancellationToken ct = default)
    {
        var exists = await db.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, ct);

        if (!exists)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveFromRoleAsync(string roleId, int permissionId, CancellationToken ct = default)
    {
        var entry = await db.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, ct);

        if (entry is not null)
        {
            db.RolePermissions.Remove(entry);
            await db.SaveChangesAsync(ct);
        }
    }
}

using Microsoft.AspNetCore.Identity;
using BKM.Utility.Application.Features.Auth.DTOs;
using BKM.Utility.Application.Features.Auth.Interfaces;
using BKM.Utility.Domain.Features.Auth.Entities;

namespace BKM.Utility.Application.Features.Auth.Services;

/// <summary>
/// Admin service for listing users, assigning/removing roles, and activating/deactivating accounts.
/// </summary>
public sealed class UserManagementService(
    UserManager<AppUser> userManager) : IUserManagementService
{
    public async Task<(IReadOnlyList<UserDto> Items, int Total)> GetUsersAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var all   = userManager.Users.OrderBy(u => u.CreatedAt).ToList();
        var total = all.Count;
        var paged = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var dtos = new List<UserDto>(paged.Count);
        foreach (var u in paged)
        {
            var roles = await userManager.GetRolesAsync(u);
            dtos.Add(ToDto(u, roles));
        }

        return (dtos.AsReadOnly(), total);
    }

    public async Task<UserDto> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        var user  = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");
        var roles = await userManager.GetRolesAsync(user);
        return ToDto(user, roles);
    }

    public async Task<UserDto> AssignRoleAsync(
        string userId, AssignRoleRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        var already = await userManager.IsInRoleAsync(user, request.RoleName);
        if (!already)
        {
            var result = await userManager.AddToRoleAsync(user, request.RoleName);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var roles = await userManager.GetRolesAsync(user);
        return ToDto(user, roles);
    }

    public async Task<UserDto> RemoveRoleAsync(
        string userId, string roleName, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));

        var roles = await userManager.GetRolesAsync(user);
        return ToDto(user, roles);
    }

    public async Task<bool> DeactivateUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;
        user.IsActive = false;
        await userManager.UpdateAsync(user);
        return true;
    }

    public async Task<bool> ActivateUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;
        user.IsActive = true;
        await userManager.UpdateAsync(user);
        return true;
    }

    private static UserDto ToDto(AppUser u, IList<string> roles) => new()
    {
        Id          = u.Id,
        Email       = u.Email ?? "",
        DisplayName = u.DisplayName,
        IsActive    = u.IsActive,
        CreatedAt   = u.CreatedAt,
        LastLoginAt = u.LastLoginAt,
        Roles       = roles
    };
}

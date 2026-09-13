using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.Auth.DTOs;
using BKM.Utility.Application.Features.Auth.Interfaces;

namespace BKM.Utility.Api.Features.Auth;

/// <summary>
/// Admin-only role management endpoints.
///
///   GET    /api/roles                                    — list all roles
///   POST   /api/roles                                    — create a role
///   DELETE /api/roles/{roleId}                           — delete a role
///   POST   /api/roles/{roleId}/permissions/{permId}      — assign permission to role
///   DELETE /api/roles/{roleId}/permissions/{permId}      — remove permission from role
/// </summary>
[ApiController]
[Route("api/roles")]
[Authorize(Policy = "AdminOnly")]
public sealed class RoleManagementController(IRoleManagementService roleManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), 200)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await roleManagementService.GetRolesAsync(ct);
        return Ok(ApiResponseBuilder.Ok(result, $"{result.Count} role(s) found."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var result = await roleManagementService.CreateRoleAsync(request.Name, request.Description, ct);
        return StatusCode(201, ApiResponseBuilder.Ok(result, "Role created."));
    }

    [HttpDelete("{roleId}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteRole(string roleId, CancellationToken ct)
    {
        var ok = await roleManagementService.DeleteRoleAsync(roleId, ct);
        if (!ok) return NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, $"Role '{roleId}' not found."));
        return Ok(ApiResponseBuilder.Ok<object?>(null, "Role deleted."));
    }

    [HttpPost("{roleId}/permissions/{permissionId:int}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignPermission(string roleId, int permissionId, CancellationToken ct)
    {
        var result = await roleManagementService.AssignPermissionAsync(roleId, permissionId, ct);
        return Ok(ApiResponseBuilder.Ok(result, "Permission assigned."));
    }

    [HttpDelete("{roleId}/permissions/{permissionId:int}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemovePermission(string roleId, int permissionId, CancellationToken ct)
    {
        var result = await roleManagementService.RemovePermissionAsync(roleId, permissionId, ct);
        return Ok(ApiResponseBuilder.Ok(result, "Permission removed."));
    }
}

/// <summary>Request body for creating a role.</summary>
public sealed class CreateRoleRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string  Name        { get; set; } = "";
    public string? Description { get; set; }
}

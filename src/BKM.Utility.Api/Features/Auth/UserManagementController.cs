using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.Auth.DTOs;
using BKM.Utility.Application.Features.Auth.Interfaces;

namespace BKM.Utility.Api.Features.Auth;

/// <summary>
/// Admin-only user management endpoints.
///
///   GET    /api/users                        — paginated user list
///   GET    /api/users/{userId}               — get user by ID
///   POST   /api/users/{userId}/roles         — assign role to user
///   DELETE /api/users/{userId}/roles/{role}  — remove role from user
///   PUT    /api/users/{userId}/deactivate    — deactivate user
///   PUT    /api/users/{userId}/activate      — activate user
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class UserManagementController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), 200)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var (items, total) = await userManagementService.GetUsersAsync(page, pageSize, ct);
        return Ok(ApiResponseBuilder<IReadOnlyList<UserDto>>
            .Success(items)
            .WithMeta("totalCount", total)
            .WithMeta("page",       page)
            .WithMeta("pageSize",   pageSize)
            .Build());
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUser(string userId, CancellationToken ct)
    {
        var result = await userManagementService.GetUserByIdAsync(userId, ct);
        return Ok(ApiResponseBuilder.Ok(result));
    }

    [HttpPost("{userId}/roles")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignRole(
        string userId, [FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        var result = await userManagementService.AssignRoleAsync(userId, request, ct);
        return Ok(ApiResponseBuilder.Ok(result, $"Role '{request.RoleName}' assigned."));
    }

    [HttpDelete("{userId}/roles/{roleName}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveRole(string userId, string roleName, CancellationToken ct)
    {
        var result = await userManagementService.RemoveRoleAsync(userId, roleName, ct);
        return Ok(ApiResponseBuilder.Ok(result, $"Role '{roleName}' removed."));
    }

    [HttpPut("{userId}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Deactivate(string userId, CancellationToken ct)
    {
        var ok = await userManagementService.DeactivateUserAsync(userId, ct);
        if (!ok) return NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, $"User '{userId}' not found."));
        return Ok(ApiResponseBuilder.Ok<object?>(null, "User deactivated."));
    }

    [HttpPut("{userId}/activate")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Activate(string userId, CancellationToken ct)
    {
        var ok = await userManagementService.ActivateUserAsync(userId, ct);
        if (!ok) return NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, $"User '{userId}' not found."));
        return Ok(ApiResponseBuilder.Ok<object?>(null, "User activated."));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.Auth.DTOs;
using BKM.Utility.Application.Features.Auth.Interfaces;

namespace BKM.Utility.Api.Features.Auth;

/// <summary>
/// Admin-only permission management endpoints.
///
///   GET    /api/permissions          — list all permissions
///   POST   /api/permissions          — create a permission
///   PUT    /api/permissions/{id}     — update a permission
///   DELETE /api/permissions/{id}     — delete a permission
/// </summary>
[ApiController]
[Route("api/permissions")]
[Authorize(Policy = "AdminOnly")]
public sealed class PermissionController(IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await permissionService.GetAllAsync(ct);
        return Ok(ApiResponseBuilder.Ok(result, $"{result.Count} permission(s) found."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PermissionDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create(
        [FromBody] UpsertPermissionRequest request, CancellationToken ct)
    {
        var result = await permissionService.UpsertAsync(request, id: null, ct);
        return StatusCode(201, ApiResponseBuilder.Ok(result, "Permission created."));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PermissionDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpsertPermissionRequest request, CancellationToken ct)
    {
        var result = await permissionService.UpsertAsync(request, id, ct);
        return Ok(ApiResponseBuilder.Ok(result, "Permission updated."));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await permissionService.DeleteAsync(id, ct);
        if (!ok) return NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, $"Permission {id} not found."));
        return Ok(ApiResponseBuilder.Ok<object?>(null, $"Permission {id} deleted."));
    }
}

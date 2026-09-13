using Microsoft.AspNetCore.Mvc;
using BKM.Integration.Application.Features.ApiResponse.Builders;
using BKM.Integration.Application.Features.ApiResponse.Models;
using BKM.Integration.Application.Features.AppLog.DTOs;
using BKM.Integration.Application.Features.AppLog.Interfaces;

namespace BKM.Integration.Api.Features.AppLog;

[ApiController]
[Route("api/logs")]
public sealed class AppLogController(IAppLogService logService) : ControllerBase
{
    /// <summary>Query application log entries with optional filters and pagination.</summary>
    /// <remarks>
    ///     GET /api/logs?level=Error&amp;feature=Encryption&amp;page=1&amp;pageSize=20
    /// </remarks>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<AppLogEntryDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Query(
        [FromQuery] AppLogQueryRequest request,
        CancellationToken ct)
    {
        var (items, total) = await logService.QueryAsync(request, ct);

        return Ok(ApiResponseBuilder<IReadOnlyList<AppLogEntryDto>>
            .Success(items)
            .WithMessage($"Retrieved {items.Count} log entries.")
            .WithMeta("totalCount",   total)
            .WithMeta("page",         request.Page)
            .WithMeta("pageSize",     Math.Clamp(request.PageSize, 1, 200))
            .WithMeta("totalPages",   (int)Math.Ceiling(total / (double)Math.Clamp(request.PageSize, 1, 200)))
            .Build());
    }
}

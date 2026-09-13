using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.AppLog.DTOs;
using BKM.Utility.Application.Features.AppLog.Interfaces;

namespace BKM.Utility.Api.Features.AppLog;

[ApiController]
[Route("api/logs/retention")]
public sealed class LogRetentionController(IRetentionPolicyService policyService) : ControllerBase
{
    // ── GET /api/logs/retention ────────────────────────────────────────────────

    /// <summary>Get all log retention policies (per-feature + default).</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<RetentionPolicyDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var policies = await policyService.GetAllAsync(ct);

        return Ok(ApiResponseBuilder<IReadOnlyList<RetentionPolicyDto>>
            .Success(policies)
            .WithMessage($"Retrieved {policies.Count} retention policies.")
            .WithMeta("tip", "Feature = null means 'default' — applies to all features with no specific policy.")
            .Build());
    }

    // ── PUT /api/logs/retention ────────────────────────────────────────────────

    /// <summary>
    /// Create or update a retention policy for a specific feature (or the default policy).
    /// </summary>
    /// <remarks>
    /// Examples:
    ///
    ///     PUT /api/logs/retention
    ///     { "feature": "UrlShortener", "dbRetentionDays": 7, "fileRetentionDays": 7 }
    ///
    ///     PUT /api/logs/retention
    ///     { "feature": null, "dbRetentionDays": 30, "fileRetentionDays": 30, "description": "Default policy" }
    ///
    ///     PUT /api/logs/retention
    ///     { "feature": "Encryption", "dbRetentionDays": 90, "fileRetentionDays": 60 }
    ///
    /// Set dbRetentionDays or fileRetentionDays to 0 to keep that type of log forever.
    /// </remarks>
    [HttpPut]
    [ProducesResponseType<ApiResponse<RetentionPolicyDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertRetentionPolicyRequest request,
        CancellationToken ct)
    {
        var policy = await policyService.UpsertAsync(request, ct);
        var label  = policy.Feature ?? "default";

        return Ok(ApiResponseBuilder.Ok(policy, $"Retention policy for '{label}' saved successfully."));
    }
}

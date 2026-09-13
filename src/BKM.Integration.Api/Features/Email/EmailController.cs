using Microsoft.AspNetCore.Mvc;
using BKM.Integration.Application.Features.ApiResponse.Builders;
using BKM.Integration.Application.Features.ApiResponse.Models;
using BKM.Integration.Application.Features.Email.DTOs;
using BKM.Integration.Application.Features.Email.Interfaces;

namespace BKM.Integration.Api.Features.Email;

/// <summary>
/// Email Notification API.
///
/// Config management:
///   GET    /api/email/config              — list all SMTP configurations
///   POST   /api/email/config              — create a new SMTP configuration
///   PUT    /api/email/config/{id}         — update an existing configuration
///   PUT    /api/email/config/{id}/activate — make this config the active one
///   DELETE /api/email/config/{id}         — delete a configuration
///
/// Template management:
///   GET    /api/email/templates           — list all templates
///   POST   /api/email/templates           — create a template
///   PUT    /api/email/templates/{id}      — update a template
///   DELETE /api/email/templates/{id}      — delete a template
///
/// Send:
///   POST   /api/email/send/template       — send via named template
///   POST   /api/email/send/raw            — send a one-off email
///   POST   /api/email/test                — SMTP connection test
///
/// Audit log:
///   GET    /api/email/audit               — paginated audit log with filters
/// </summary>
[ApiController]
[Route("api/email")]
public sealed class EmailController(IEmailService emailService) : ControllerBase
{
    // ── Config ─────────────────────────────────────────────────────────────────

    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmailConfigDto>>), 200)]
    public async Task<IActionResult> GetConfigs(CancellationToken ct)
    {
        var result = await emailService.GetConfigsAsync(ct);
        return Ok(ApiResponseBuilder.Ok(result, $"{result.Count} configuration(s) found."));
    }

    [HttpPost("config")]
    [ProducesResponseType(typeof(ApiResponse<EmailConfigDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateConfig([FromBody] UpsertEmailConfigRequest request, CancellationToken ct)
    {
        var result = await emailService.UpsertConfigAsync(request, id: null, ct);
        return CreatedAtAction(nameof(GetConfigs), ApiResponseBuilder.Ok(result, "Email configuration created."));
    }

    [HttpPut("config/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmailConfigDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateConfig(int id, [FromBody] UpsertEmailConfigRequest request, CancellationToken ct)
    {
        var result = await emailService.UpsertConfigAsync(request, id, ct);
        return Ok(ApiResponseBuilder.Ok(result, "Email configuration updated."));
    }

    [HttpPut("config/{id:int}/activate")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ActivateConfig(int id, CancellationToken ct)
    {
        var ok = await emailService.SetActiveConfigAsync(id, ct);
        if (!ok) return NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, $"EmailConfig {id} not found."));
        return Ok(ApiResponseBuilder.Ok<object?>(null, $"Configuration {id} is now active."));
    }

    [HttpDelete("config/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteConfig(int id, CancellationToken ct)
    {
        var ok = await emailService.DeleteConfigAsync(id, ct);
        if (!ok) return NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, $"EmailConfig {id} not found."));
        return Ok(ApiResponseBuilder.Ok<object?>(null, $"Configuration {id} deleted."));
    }

    // ── Templates ──────────────────────────────────────────────────────────────

    [HttpGet("templates")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmailTemplateDto>>), 200)]
    public async Task<IActionResult> GetTemplates(CancellationToken ct)
    {
        var result = await emailService.GetTemplatesAsync(ct);
        return Ok(ApiResponseBuilder.Ok(result, $"{result.Count} template(s) found."));
    }

    [HttpPost("templates")]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateTemplate([FromBody] UpsertEmailTemplateRequest request, CancellationToken ct)
    {
        var result = await emailService.UpsertTemplateAsync(request, id: null, ct);
        return CreatedAtAction(nameof(GetTemplates), ApiResponseBuilder.Ok(result, "Template created."));
    }

    [HttpPut("templates/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmailTemplateDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpsertEmailTemplateRequest request, CancellationToken ct)
    {
        var result = await emailService.UpsertTemplateAsync(request, id, ct);
        return Ok(ApiResponseBuilder.Ok(result, "Template updated."));
    }

    [HttpDelete("templates/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteTemplate(int id, CancellationToken ct)
    {
        var ok = await emailService.DeleteTemplateAsync(id, ct);
        if (!ok) return NotFound(ApiResponseBuilder.Error(ErrorCodes.NotFound, $"Template {id} not found."));
        return Ok(ApiResponseBuilder.Ok<object?>(null, $"Template {id} deleted."));
    }

    // ── Send ───────────────────────────────────────────────────────────────────

    [HttpPost("send/template")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SendTemplate([FromBody] SendTemplateEmailRequest request, CancellationToken ct)
    {
        await emailService.SendAsync(request, ct);
        return Ok(ApiResponseBuilder.Ok<object?>(null, $"Email sent to {request.To}."));
    }

    [HttpPost("send/raw")]
    [ProducesResponseType(typeof(ApiResponse<object?>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SendRaw([FromBody] SendRawEmailRequest request, CancellationToken ct)
    {
        await emailService.SendRawAsync(request, ct);
        return Ok(ApiResponseBuilder.Ok<object?>(null, $"Email sent to {request.To}."));
    }

    [HttpPost("test")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> TestConnection([FromQuery] string to, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(to))
            return BadRequest(ApiResponseBuilder.Error(ErrorCodes.BadRequest, "Query parameter 'to' is required."));

        var message = await emailService.TestConnectionAsync(to, ct);
        return Ok(ApiResponseBuilder.Ok(message, "Test completed."));
    }

    // ── Audit ──────────────────────────────────────────────────────────────────

    [HttpGet("audit")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmailAuditLogDto>>), 200)]
    public async Task<IActionResult> GetAudit([FromQuery] EmailAuditQueryRequest request, CancellationToken ct)
    {
        var (items, total) = await emailService.GetAuditLogsAsync(request, ct);
        return Ok(ApiResponseBuilder<IReadOnlyList<EmailAuditLogDto>>
            .Success(items)
            .WithMeta("totalCount", total)
            .WithMeta("page",       request.Page)
            .WithMeta("pageSize",   request.PageSize)
            .Build());
    }
}

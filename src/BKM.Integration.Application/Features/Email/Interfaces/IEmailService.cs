using BKM.Integration.Application.Features.Email.DTOs;

namespace BKM.Integration.Application.Features.Email.Interfaces;

/// <summary>
/// Application-level email service.
/// Orchestrates config loading (cache-aside), template rendering, sending, and audit logging.
/// </summary>
public interface IEmailService
{
    // ── Send operations ────────────────────────────────────────────────────────

    /// <summary>Send an email using a named template. {{Key}} tokens are replaced from variables.</summary>
    Task SendAsync(SendTemplateEmailRequest request, CancellationToken ct = default);

    /// <summary>Send a one-off email without a template.</summary>
    Task SendRawAsync(SendRawEmailRequest request, CancellationToken ct = default);

    /// <summary>Send a test email to verify the active configuration works.</summary>
    Task<string> TestConnectionAsync(string toAddress, CancellationToken ct = default);

    // ── Config management ──────────────────────────────────────────────────────

    Task<IReadOnlyList<EmailConfigDto>> GetConfigsAsync(CancellationToken ct = default);
    Task<EmailConfigDto> UpsertConfigAsync(UpsertEmailConfigRequest request, int? id = null, CancellationToken ct = default);
    Task<bool> SetActiveConfigAsync(int id, CancellationToken ct = default);
    Task<bool> DeleteConfigAsync(int id, CancellationToken ct = default);

    // ── Template management ────────────────────────────────────────────────────

    Task<IReadOnlyList<EmailTemplateDto>> GetTemplatesAsync(CancellationToken ct = default);
    Task<EmailTemplateDto> UpsertTemplateAsync(UpsertEmailTemplateRequest request, int? id = null, CancellationToken ct = default);
    Task<bool> DeleteTemplateAsync(int id, CancellationToken ct = default);

    // ── Audit log ──────────────────────────────────────────────────────────────

    Task<(IReadOnlyList<EmailAuditLogDto> Items, int TotalCount)> GetAuditLogsAsync(
        EmailAuditQueryRequest request, CancellationToken ct = default);
}

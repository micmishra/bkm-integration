using System.Diagnostics;
using System.Text.RegularExpressions;
using BKM.Utility.Application.Features.Email.DTOs;
using BKM.Utility.Application.Features.Email.Interfaces;
using BKM.Utility.Domain.Features.Email.Entities;
using BKM.Utility.Domain.Features.Email.Interfaces;
using BKM.Utility.Domain.Features.Encryption.Interfaces;
using BKM.Utility.Domain.Shared.Interfaces;

namespace BKM.Utility.Application.Features.Email.Services;

/// <summary>
/// Orchestrates config (DB-stored, cache-aside 5 min), template rendering,
/// SMTP send (via ISmtpSender), and audit logging.
///
/// Security:
///   - Passwords stored AES-256-GCM encrypted via IEncryptionService
///   - Config is never returned with PasswordCipher
///   - All sends go through ISmtpSender which enforces TLS
///
/// Cache:
///   - Active config cached 5 min under key "email:config:active"
///   - Template cached 5 min under key "email:template:{name}"
///   - Cache evicted on config/template updates
/// </summary>
public sealed class EmailService(
    IEmailConfigRepository   configRepo,
    IEmailTemplateRepository templateRepo,
    IEmailAuditLogRepository auditRepo,
    IEncryptionService       encryption,
    ISmtpSender              sender,
    ICacheService<EmailConfig>   configCache,
    ICacheService<EmailTemplate> templateCache) : IEmailService
{
    private static readonly TimeSpan ConfigCacheTtl   = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TemplateCacheTtl = TimeSpan.FromMinutes(5);
    private const string ActiveConfigKey = "email:config:active";
    private static string TemplateKey(string name) => $"email:template:{name.ToLowerInvariant()}";

    // ── Send operations ────────────────────────────────────────────────────────

    public async Task SendAsync(SendTemplateEmailRequest request, CancellationToken ct = default)
    {
        var config   = await GetActiveConfigInternalAsync(ct);
        var template = await GetTemplateInternalAsync(request.TemplateName, ct);

        var subject = Render(template.Subject, request.Variables);
        var body    = Render(template.Body,    request.Variables);

        await SendAndAuditAsync(config, request.To, subject, body, template.IsHtml,
            request.TemplateName, ct);
    }

    public async Task SendRawAsync(SendRawEmailRequest request, CancellationToken ct = default)
    {
        var config = await GetActiveConfigInternalAsync(ct);
        await SendAndAuditAsync(config, request.To, request.Subject, request.Body,
            request.IsHtml, templateName: null, ct);
    }

    public async Task<string> TestConnectionAsync(string toAddress, CancellationToken ct = default)
    {
        var config = await GetActiveConfigInternalAsync(ct);
        var sw     = Stopwatch.StartNew();
        try
        {
            await sender.SendAsync(config, toAddress,
                subject: "BKM Integration — SMTP Test",
                body:    $"SMTP connection test from BKM.Utility at {DateTime.UtcNow:O}. If you received this, the configuration is working.",
                isHtml:  false, ct);
            sw.Stop();
            return $"Test email sent successfully in {sw.ElapsedMilliseconds} ms.";
        }
        catch (Exception ex)
        {
            sw.Stop();
            return $"Test failed after {sw.ElapsedMilliseconds} ms: {ex.Message}";
        }
    }

    // ── Config management ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<EmailConfigDto>> GetConfigsAsync(CancellationToken ct = default)
        => (await configRepo.GetAllAsync(ct)).Select(MapConfig).ToList();

    public async Task<EmailConfigDto> UpsertConfigAsync(
        UpsertEmailConfigRequest request, int? id = null, CancellationToken ct = default)
    {
        EmailConfig config;

        if (id.HasValue)
        {
            config = await configRepo.GetByIdAsync(id.Value, ct)
                ?? throw new KeyNotFoundException($"EmailConfig {id} not found.");

            // Only re-encrypt password if a new one is provided
            if (!string.IsNullOrWhiteSpace(request.Password))
                config.PasswordCipher = encryption.Encrypt(request.Password);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required when creating a new email configuration.");

            config = new EmailConfig
            {
                PasswordCipher = encryption.Encrypt(request.Password),
                CreatedAt      = DateTime.UtcNow
            };
        }

        config.Name           = request.Name;
        config.Host           = request.Host;
        config.Port           = request.Port;
        config.TlsMode        = request.TlsMode;
        config.Username       = request.Username;
        config.FromAddress    = request.FromAddress;
        config.FromName       = request.FromName;
        config.TimeoutSeconds = request.TimeoutSeconds;
        config.UpdatedAt      = DateTime.UtcNow;

        var saved = await configRepo.SaveAsync(config, ct);

        // Evict cached active config so next send picks up the change
        await configCache.RemoveAsync(ActiveConfigKey, ct);

        return MapConfig(saved);
    }

    public async Task<bool> SetActiveConfigAsync(int id, CancellationToken ct = default)
    {
        var result = await configRepo.SetActiveAsync(id, ct);
        await configCache.RemoveAsync(ActiveConfigKey, ct);
        return result;
    }

    public async Task<bool> DeleteConfigAsync(int id, CancellationToken ct = default)
    {
        var result = await configRepo.DeleteAsync(id, ct);
        await configCache.RemoveAsync(ActiveConfigKey, ct);
        return result;
    }

    // ── Template management ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<EmailTemplateDto>> GetTemplatesAsync(CancellationToken ct = default)
        => (await templateRepo.GetAllAsync(ct)).Select(MapTemplate).ToList();

    public async Task<EmailTemplateDto> UpsertTemplateAsync(
        UpsertEmailTemplateRequest request, int? id = null, CancellationToken ct = default)
    {
        EmailTemplate template;

        if (id.HasValue)
        {
            template = await templateRepo.GetByIdAsync(id.Value, ct)
                ?? throw new KeyNotFoundException($"EmailTemplate {id} not found.");
        }
        else
        {
            template = new EmailTemplate { CreatedAt = DateTime.UtcNow };
        }

        template.Name        = request.Name;
        template.Subject     = request.Subject;
        template.Body        = request.Body;
        template.IsHtml      = request.IsHtml;
        template.IsActive    = request.IsActive;
        template.Description = request.Description;
        template.UpdatedAt   = DateTime.UtcNow;

        var saved = await templateRepo.SaveAsync(template, ct);

        // Evict cached template
        await templateCache.RemoveAsync(TemplateKey(saved.Name), ct);

        return MapTemplate(saved);
    }

    public async Task<bool> DeleteTemplateAsync(int id, CancellationToken ct = default)
    {
        var existing = await templateRepo.GetByIdAsync(id, ct);
        var result   = await templateRepo.DeleteAsync(id, ct);
        if (existing is not null)
            await templateCache.RemoveAsync(TemplateKey(existing.Name), ct);
        return result;
    }

    // ── Audit log ──────────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<EmailAuditLogDto> Items, int TotalCount)> GetAuditLogsAsync(
        EmailAuditQueryRequest request, CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var page     = Math.Max(request.Page, 1);

        var items = await auditRepo.QueryAsync(
            request.ToAddress, request.Success, request.From, request.To, page, pageSize, ct);
        var total = await auditRepo.CountAsync(
            request.ToAddress, request.Success, request.From, request.To, ct);

        return (items.Select(MapAudit).ToList(), total);
    }

    // ── Internal helpers ───────────────────────────────────────────────────────

    private async Task<EmailConfig> GetActiveConfigInternalAsync(CancellationToken ct)
    {
        var cached = await configCache.GetAsync(ActiveConfigKey, ct);
        if (cached is not null) return cached;

        var config = await configRepo.GetActiveAsync(ct)
            ?? throw new InvalidOperationException(
                "No active email configuration found. Configure SMTP settings via PUT /api/email/config.");

        await configCache.SetAsync(ActiveConfigKey, config, ConfigCacheTtl, ct);
        return config;
    }

    private async Task<EmailTemplate> GetTemplateInternalAsync(string name, CancellationToken ct)
    {
        var key    = TemplateKey(name);
        var cached = await templateCache.GetAsync(key, ct);
        if (cached is not null) return cached;

        var template = await templateRepo.GetByNameAsync(name, ct)
            ?? throw new KeyNotFoundException($"Email template '{name}' not found or inactive.");

        if (!template.IsActive)
            throw new InvalidOperationException($"Email template '{name}' is disabled.");

        await templateCache.SetAsync(key, template, TemplateCacheTtl, ct);
        return template;
    }

    private async Task SendAndAuditAsync(
        EmailConfig config, string to, string subject, string body,
        bool isHtml, string? templateName, CancellationToken ct)
    {
        var sw      = Stopwatch.StartNew();
        var success = false;
        string? error = null;

        try
        {
            // Decrypt password just-in-time — never held in memory longer than the send.
            // We pass a transient copy so the cached config's cipher is never overwritten.
            var plainPassword = encryption.Decrypt(config.PasswordCipher);
            var runtimeConfig = new EmailConfig
            {
                Id             = config.Id,
                Name           = config.Name,
                Host           = config.Host,
                Port           = config.Port,
                TlsMode        = config.TlsMode,
                Username       = config.Username,
                PasswordCipher = plainPassword,   // plain-text for the live send only
                FromAddress    = config.FromAddress,
                FromName       = config.FromName,
                TimeoutSeconds = config.TimeoutSeconds,
                IsActive       = config.IsActive,
                CreatedAt      = config.CreatedAt,
                UpdatedAt      = config.UpdatedAt
            };

            await sender.SendAsync(runtimeConfig, to, subject, body, isHtml, ct);
            success = true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            throw;  // Re-throw so caller / middleware sees the failure
        }
        finally
        {
            sw.Stop();
            // Audit write is fire-and-forget — never fails the caller
            _ = auditRepo.WriteAsync(new EmailAuditLog
            {
                ToAddress    = to,
                Subject      = subject,
                TemplateName = templateName,
                Success      = success,
                ErrorMessage = error,
                DurationMs   = sw.ElapsedMilliseconds,
                SentAt       = DateTime.UtcNow
            }, CancellationToken.None);
        }
    }

    /// <summary>
    /// Replaces {{Key}} tokens in the template string with values from the dictionary.
    /// Unknown tokens are left unchanged.
    /// </summary>
    private static string Render(string template, Dictionary<string, string> variables)
    {
        if (variables.Count == 0) return template;
        return Regex.Replace(template, @"\{\{(\w+)\}\}", m =>
            variables.TryGetValue(m.Groups[1].Value, out var val) ? val : m.Value);
    }

    // ── Mappers ────────────────────────────────────────────────────────────────

    private static EmailConfigDto MapConfig(EmailConfig c) => new()
    {
        Id             = c.Id,
        Name           = c.Name,
        Host           = c.Host,
        Port           = c.Port,
        TlsMode        = c.TlsMode,
        Username       = c.Username,
        FromAddress    = c.FromAddress,
        FromName       = c.FromName,
        TimeoutSeconds = c.TimeoutSeconds,
        IsActive       = c.IsActive,
        CreatedAt      = c.CreatedAt,
        UpdatedAt      = c.UpdatedAt
    };

    private static EmailTemplateDto MapTemplate(EmailTemplate t) => new()
    {
        Id          = t.Id,
        Name        = t.Name,
        Subject     = t.Subject,
        Body        = t.Body,
        IsHtml      = t.IsHtml,
        IsActive    = t.IsActive,
        Description = t.Description,
        CreatedAt   = t.CreatedAt,
        UpdatedAt   = t.UpdatedAt
    };

    private static EmailAuditLogDto MapAudit(EmailAuditLog a) => new()
    {
        Id           = a.Id,
        ToAddress    = a.ToAddress,
        Subject      = a.Subject,
        TemplateName = a.TemplateName,
        Success      = a.Success,
        ErrorMessage = a.ErrorMessage,
        DurationMs   = a.DurationMs,
        SentAt       = a.SentAt
    };
}

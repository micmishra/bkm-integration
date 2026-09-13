namespace BKM.Integration.Domain.Features.Email.Entities;

/// <summary>
/// SMTP server configuration stored in dbo.EmailConfigs.
/// Only one row can be active at a time (IsActive flag).
/// Credentials (password) are stored AES-256-GCM encrypted via IEncryptionService.
/// </summary>
public sealed class EmailConfig
{
    public int     Id            { get; set; }

    /// <summary>Human-readable label, e.g. "Production SMTP", "Dev Relay"</summary>
    public string  Name          { get; set; } = string.Empty;

    public string  Host          { get; set; } = string.Empty;
    public int     Port          { get; set; } = 465;

    /// <summary>
    /// TLS mode: SslOnConnect (port 465) or StartTls (port 587).
    /// Plain-text (port 25) is intentionally not supported — it fails Veracode CWE-319.
    /// </summary>
    public SmtpTlsMode TlsMode  { get; set; } = SmtpTlsMode.SslOnConnect;

    public string  Username      { get; set; } = string.Empty;

    /// <summary>AES-256-GCM encrypted cipherPackage. Never stored in plain text.</summary>
    public string  PasswordCipher { get; set; } = string.Empty;

    public string  FromAddress   { get; set; } = string.Empty;
    public string  FromName      { get; set; } = string.Empty;

    /// <summary>Timeout in seconds for SMTP connection + commands.</summary>
    public int     TimeoutSeconds { get; set; } = 30;

    public bool    IsActive      { get; set; } = true;
    public DateTime CreatedAt    { get; set; }
    public DateTime UpdatedAt    { get; set; }
}

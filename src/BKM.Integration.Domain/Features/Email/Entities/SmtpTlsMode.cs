namespace BKM.Integration.Domain.Features.Email.Entities;

/// <summary>
/// Controls how TLS is negotiated.
/// Plain-text mode is intentionally omitted — it fails Veracode CWE-319 (cleartext transmission).
/// </summary>
public enum SmtpTlsMode
{
    /// <summary>
    /// Implicit TLS on connect (port 465). TLS handshake happens before any SMTP commands.
    /// Most secure. Recommended default.
    /// </summary>
    SslOnConnect = 0,

    /// <summary>
    /// STARTTLS upgrade (port 587). Starts plain-text then upgrades to TLS via STARTTLS command.
    /// Still secure when enforced — our sender rejects the session if STARTTLS is not advertised.
    /// </summary>
    StartTls = 1
}

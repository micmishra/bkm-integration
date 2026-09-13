using BKM.Integration.Domain.Features.Email.Entities;

namespace BKM.Integration.Application.Features.Email.Interfaces;

/// <summary>
/// Low-level SMTP send abstraction.
/// Concrete implementation lives in Infrastructure (SmtpSender) using pure BCL SslStream/TcpClient.
/// Keeping this interface in Application lets EmailService stay free of Infrastructure knowledge.
/// </summary>
public interface ISmtpSender
{
    /// <summary>
    /// Sends a single email message.
    /// The implementation must use TLS (SslOnConnect or StartTls only — no plain-text).
    /// </summary>
    Task SendAsync(
        EmailConfig  config,
        string       toAddress,
        string       subject,
        string       body,
        bool         isHtml,
        CancellationToken ct = default);
}

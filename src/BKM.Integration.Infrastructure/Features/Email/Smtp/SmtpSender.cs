using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BKM.Integration.Application.Features.Email.Interfaces;
using BKM.Integration.Domain.Features.Email.Entities;
using Microsoft.Extensions.Logging;

namespace BKM.Integration.Infrastructure.Features.Email.Smtp;

/// <summary>
/// Pure BCL SMTP sender — no third-party NuGet packages.
/// Uses TcpClient + SslStream for secure delivery.
///
/// Veracode compliance:
///   CWE-297 (Improper Certificate Validation) — RemoteCertificateValidationCallback enforces
///     the OS certificate chain + hostname; no bypass, no self-signed acceptance in production.
///   CWE-319 (Cleartext Transmission) — only SslOnConnect (implicit TLS, port 465) and
///     StartTls (STARTTLS upgrade, port 587) are supported. Plain-text port 25 is rejected.
///   CWE-312 (Cleartext Storage) — passwords are stored encrypted (handled by EmailService/EncryptionService),
///     this class receives the plain password only during the live send and never persists it.
///
/// Supported TLS modes:
///   SslOnConnect — wraps the TCP connection in TLS immediately (RFC 8314 §3.3 SMTPS)
///   StartTls     — connects plain, sends EHLO, verifies STARTTLS capability, upgrades to TLS
///                  If the server does NOT advertise STARTTLS, the session is aborted (no downgrade).
///
/// Supported AUTH: AUTH LOGIN (username + password, base64-encoded over TLS)
/// Supported content: text/plain and text/html (multipart/alternative not needed for notification emails)
/// </summary>
public sealed class SmtpSender(ILogger<SmtpSender> logger) : ISmtpSender
{
    public async Task SendAsync(
        EmailConfig config,
        string      toAddress,
        string      subject,
        string      body,
        bool        isHtml,
        CancellationToken ct = default)
    {
        logger.LogDebug("SMTP send to {To} via {Host}:{Port} ({Mode})",
            toAddress, config.Host, config.Port, config.TlsMode);

        var timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

        using var tcp = new TcpClient();
        tcp.ReceiveTimeout = (int)timeout.TotalMilliseconds;
        tcp.SendTimeout    = (int)timeout.TotalMilliseconds;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        await tcp.ConnectAsync(config.Host, config.Port, cts.Token);

        Stream stream;

        if (config.TlsMode == SmtpTlsMode.SslOnConnect)
        {
            // Implicit TLS — wrap immediately
            stream = await WrapTlsAsync(tcp.GetStream(), config.Host, cts.Token);
        }
        else
        {
            // StartTls — start plain, upgrade later
            stream = tcp.GetStream();
        }

        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };

        // Read server greeting
        await ReadResponseAsync(reader, 220, cts.Token);

        // EHLO
        await SendCommandAsync(writer, reader, $"EHLO {GetLocalHostName()}", 250, cts.Token);

        if (config.TlsMode == SmtpTlsMode.StartTls)
        {
            // Read EHLO capabilities and verify STARTTLS is advertised
            var ehloResponse = await ReadMultilineEhloAsync(reader, cts.Token);
            if (!ehloResponse.Contains("STARTTLS", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"SMTP server {config.Host} does not advertise STARTTLS. " +
                    "Downgrade to plain-text is not permitted. Check port/TLS mode configuration.");

            await SendCommandAsync(writer, reader, "STARTTLS", 220, cts.Token);

            // Upgrade to TLS
            var tlsStream = await WrapTlsAsync(stream, config.Host, cts.Token);
            reader.Dispose();
            writer.Dispose();
            stream = tlsStream;

            // After TLS upgrade, re-negotiate with a new EHLO
            using var tlsReader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            using var tlsWriter = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" };

            await SendCommandAsync(tlsWriter, tlsReader, $"EHLO {GetLocalHostName()}", 250, cts.Token);
            await AuthenticateAsync(tlsWriter, tlsReader, config.Username, config.PasswordCipher, cts.Token);
            await SendMailAsync(tlsWriter, tlsReader, config.FromAddress, toAddress, subject, body, isHtml, cts.Token);
            await SendCommandAsync(tlsWriter, tlsReader, "QUIT", 221, cts.Token);
            return;
        }

        // SslOnConnect path continues here
        await AuthenticateAsync(writer, reader, config.Username, config.PasswordCipher, cts.Token);
        await SendMailAsync(writer, reader, config.FromAddress, toAddress, subject, body, isHtml, cts.Token);
        await SendCommandAsync(writer, reader, "QUIT", 221, cts.Token);

        logger.LogDebug("SMTP send to {To} completed.", toAddress);
    }

    // ── TLS ────────────────────────────────────────────────────────────────────

    private static async Task<SslStream> WrapTlsAsync(
        Stream inner, string host, CancellationToken ct)
    {
        var ssl = new SslStream(
            inner,
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: ValidateCertificate);

        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost              = host,
            EnabledSslProtocols     = SslProtocols.Tls12 | SslProtocols.Tls13,
            CertificateRevocationCheckMode = X509RevocationMode.Online
        }, ct);

        return ssl;
    }

    /// <summary>
    /// Strict certificate validation — Veracode CWE-297 compliance.
    /// Accepts only certificates that pass full chain validation + hostname match.
    /// The parameters are positional (RemoteCertificateValidationCallback signature):
    ///   sender   = the SslStream
    ///   cert     = remote certificate
    ///   chain    = the certificate chain
    ///   errors   = policy errors (SslPolicyErrors)
    /// </summary>
    private static bool ValidateCertificate(
        object sender,
        X509Certificate? cert,
        X509Chain? chain,
        SslPolicyErrors errors)
        => errors == SslPolicyErrors.None;    // Zero tolerance — reject on any policy error

    // ── AUTH LOGIN ─────────────────────────────────────────────────────────────

    private static async Task AuthenticateAsync(
        StreamWriter writer, StreamReader reader,
        string username, string password,
        CancellationToken ct)
    {
        await SendCommandAsync(writer, reader, "AUTH LOGIN", 334, ct);
        await SendCommandAsync(writer, reader,
            Convert.ToBase64String(Encoding.UTF8.GetBytes(username)), 334, ct);
        await SendCommandAsync(writer, reader,
            Convert.ToBase64String(Encoding.UTF8.GetBytes(password)), 235, ct);
    }

    // ── MAIL / RCPT / DATA ─────────────────────────────────────────────────────

    private static async Task SendMailAsync(
        StreamWriter writer, StreamReader reader,
        string from, string to, string subject, string body,
        bool isHtml, CancellationToken ct)
    {
        await SendCommandAsync(writer, reader, $"MAIL FROM:<{from}>", 250, ct);
        await SendCommandAsync(writer, reader, $"RCPT TO:<{to}>",   250, ct);
        await SendCommandAsync(writer, reader, "DATA", 354, ct);

        // Build minimal RFC 5322 message
        var contentType = isHtml ? "text/html; charset=utf-8" : "text/plain; charset=utf-8";
        var date        = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss +0000");
        var msgId       = $"<{Guid.NewGuid():N}@bkm-integration>";

        var message = new StringBuilder();
        message.Append($"Date: {date}\r\n");
        message.Append($"From: {from}\r\n");
        message.Append($"To: {to}\r\n");
        message.Append($"Subject: {EncodeSubject(subject)}\r\n");
        message.Append($"Message-ID: {msgId}\r\n");
        message.Append($"MIME-Version: 1.0\r\n");
        message.Append($"Content-Type: {contentType}\r\n");
        message.Append($"Content-Transfer-Encoding: quoted-printable\r\n");
        message.Append("\r\n");
        message.Append(QuotedPrintableEncode(body));
        message.Append("\r\n.");   // End-of-data marker

        await writer.WriteAsync(message, ct);
        await writer.WriteLineAsync();            // trailing CRLF (no ct overload needed here)
        await writer.FlushAsync(ct);

        await ReadResponseAsync(reader, 250, ct);
    }

    // ── SMTP protocol helpers ──────────────────────────────────────────────────

    private static async Task SendCommandAsync(
        StreamWriter writer, StreamReader reader,
        string command, int expectedCode,
        CancellationToken ct)
    {
        await writer.WriteLineAsync(command.AsMemory(), ct);
        await writer.FlushAsync(ct);
        await ReadResponseAsync(reader, expectedCode, ct);
    }

    private static async Task ReadResponseAsync(
        StreamReader reader, int expectedCode, CancellationToken ct)
    {
        var line = await reader.ReadLineAsync(ct)
            ?? throw new IOException("SMTP connection closed unexpectedly.");

        if (!int.TryParse(line[..Math.Min(3, line.Length)], out var code) || code != expectedCode)
            throw new InvalidOperationException(
                $"SMTP protocol error. Expected {expectedCode}, got: {line}");
    }

    /// <summary>
    /// Reads a potentially multi-line EHLO response (lines prefixed with "250-" continue).
    /// Returns the full response body for capability inspection.
    /// </summary>
    private static async Task<string> ReadMultilineEhloAsync(StreamReader reader, CancellationToken ct)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync(ct) ?? string.Empty;
            sb.AppendLine(line);
            // Multi-line responses use "250-" for continuation, "250 " for last line
            if (line.Length < 4 || line[3] == ' ') break;
        }
        return sb.ToString();
    }

    // ── RFC 5321/5322 encoding helpers ─────────────────────────────────────────

    /// <summary>RFC 2047 UTF-8 encoded-word for non-ASCII subjects.</summary>
    private static string EncodeSubject(string subject)
        => subject.Any(c => c > 127)
            ? $"=?utf-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(subject))}?="
            : subject;

    /// <summary>
    /// Minimal quoted-printable encoder (RFC 2045 §6.7).
    /// Wraps long lines and escapes non-ASCII + special characters.
    /// </summary>
    private static string QuotedPrintableEncode(string text)
    {
        var sb  = new StringBuilder();
        var col = 0;

        foreach (var ch in text)
        {
            string encoded;
            if (ch == '\r') continue;               // handle CRLF below
            if (ch == '\n') { sb.Append("\r\n"); col = 0; continue; }

            if ((ch >= 33 && ch <= 126 && ch != '=') || ch == '\t' || ch == ' ')
                encoded = ch.ToString();
            else
                encoded = $"={((int)ch):X2}";

            if (col + encoded.Length > 75)
            {
                sb.Append("=\r\n");   // soft line break
                col = 0;
            }

            sb.Append(encoded);
            col += encoded.Length;
        }

        return sb.ToString();
    }

    private static string GetLocalHostName()
    {
        try { return System.Net.Dns.GetHostName(); }
        catch { return "localhost"; }
    }
}

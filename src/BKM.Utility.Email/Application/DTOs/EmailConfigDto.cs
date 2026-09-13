using BKM.Utility.Domain.Features.Email.Entities;

namespace BKM.Utility.Application.Features.Email.DTOs;

public sealed class EmailConfigDto
{
    public int         Id             { get; init; }
    public string      Name           { get; init; } = string.Empty;
    public string      Host           { get; init; } = string.Empty;
    public int         Port           { get; init; }
    public SmtpTlsMode TlsMode        { get; init; }
    public string      Username       { get; init; } = string.Empty;
    public string      FromAddress    { get; init; } = string.Empty;
    public string      FromName       { get; init; } = string.Empty;
    public int         TimeoutSeconds { get; init; }
    public bool        IsActive       { get; init; }
    public DateTime    CreatedAt      { get; init; }
    public DateTime    UpdatedAt      { get; init; }
    // PasswordCipher is intentionally excluded from the DTO — never returned to callers
}

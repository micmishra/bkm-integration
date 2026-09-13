namespace BKM.Integration.Application.Features.ApiResponse.Models;

/// <summary>
/// Well-known error codes used across all features.
/// Consumers can switch on these codes without parsing message strings.
/// </summary>
public static class ErrorCodes
{
    // ── Generic ────────────────────────────────────────────────────────────────
    public const string InternalError       = "INTERNAL_ERROR";
    public const string ValidationFailed    = "VALIDATION_FAILED";
    public const string NotFound            = "NOT_FOUND";
    public const string Unauthorized        = "UNAUTHORIZED";
    public const string Forbidden           = "FORBIDDEN";
    public const string Conflict            = "CONFLICT";
    public const string BadRequest          = "BAD_REQUEST";
    public const string ServiceUnavailable  = "SERVICE_UNAVAILABLE";
    public const string Timeout             = "TIMEOUT";

    // ── UrlShortener feature ───────────────────────────────────────────────────
    public const string UrlInvalid          = "URL_INVALID";
    public const string UrlNotFound         = "URL_NOT_FOUND";

    // ── Encryption feature ─────────────────────────────────────────────────────
    public const string DecryptionFailed    = "DECRYPTION_FAILED";
    public const string EncryptionKeyMissing = "ENCRYPTION_KEY_MISSING";

    // ── Future features: add feature-specific codes below ─────────────────────
}

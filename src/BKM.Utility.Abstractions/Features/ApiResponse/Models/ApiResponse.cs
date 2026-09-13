using System.Text.Json.Serialization;

namespace BKM.Utility.Application.Features.ApiResponse.Models;

/// <summary>
/// Universal response envelope for every API endpoint in BKM.Utility.
/// All fields are always present in the JSON output — nulls are serialised as null,
/// empty collections as [], so consumers can rely on a consistent contract.
/// </summary>
/// <typeparam name="T">The type of the primary payload in <see cref="Data"/>.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>True when the operation completed without errors.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>Human-readable summary of the outcome.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>Primary response payload. Null on failure.</summary>
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    /// <summary>
    /// Pagination, counts, processing stats, or any supplemental key/value pairs.
    /// </summary>
    [JsonPropertyName("meta")]
    public Dictionary<string, object?> Meta { get; init; } = [];

    /// <summary>
    /// Non-fatal issues that did not prevent success (e.g. deprecated fields used,
    /// fallback values applied).
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; init; } = [];

    /// <summary>
    /// Structured error list. Populated on failure.
    /// Each error has a Code, Message, and optional Field for validation errors.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<ApiError> Errors { get; init; } = [];

    /// <summary>
    /// Diagnostic context included only in non-Production environments.
    /// Contains TraceId, exception type, stack trace, etc.
    /// </summary>
    [JsonPropertyName("debug")]
    public ApiDebug? Debug { get; init; }
}

/// <summary>A single structured error entry.</summary>
public sealed class ApiError
{
    /// <summary>Machine-readable error code (e.g. "VALIDATION_FAILED", "NOT_FOUND").</summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>Human-readable description of the error.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>The specific field that caused the error, if applicable.</summary>
    [JsonPropertyName("field")]
    public string? Field { get; init; }
}

/// <summary>Debug block — only populated outside Production.</summary>
public sealed class ApiDebug
{
    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonPropertyName("exceptionType")]
    public string? ExceptionType { get; init; }

    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

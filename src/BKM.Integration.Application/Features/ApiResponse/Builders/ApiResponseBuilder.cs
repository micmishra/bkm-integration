using BKM.Integration.Application.Features.ApiResponse.Models;

namespace BKM.Integration.Application.Features.ApiResponse.Builders;

/// <summary>
/// Fluent builder for constructing <see cref="ApiResponse{T}"/> instances.
///
/// Usage — success:
///   ApiResponseBuilder.Success(data)
///       .WithMessage("Created successfully.")
///       .WithMeta("totalCount", 42)
///       .Build();
///
/// Usage — failure:
///   ApiResponseBuilder.Fail()
///       .WithMessage("Validation failed.")
///       .WithError(ErrorCodes.ValidationFailed, "Email is required.", field: "email")
///       .Build();
/// </summary>
public sealed class ApiResponseBuilder<T>
{
    private bool                        _success;
    private string                      _message  = string.Empty;
    private T?                          _data;
    private readonly Dictionary<string, object?> _meta     = [];
    private readonly List<string>       _warnings = [];
    private readonly List<ApiError>     _errors   = [];
    private ApiDebug?                   _debug;

    private ApiResponseBuilder() { }

    // ── Factory entry points ───────────────────────────────────────────────────

    public static ApiResponseBuilder<T> Success(T? data = default)
        => new() { _success = true, _data = data, _message = "Request processed successfully." };

    public static ApiResponseBuilder<T> Fail()
        => new() { _success = false };

    // ── Fluent setters ─────────────────────────────────────────────────────────

    public ApiResponseBuilder<T> WithMessage(string message)
    { _message = message; return this; }

    public ApiResponseBuilder<T> WithData(T data)
    { _data = data; return this; }

    public ApiResponseBuilder<T> WithMeta(string key, object? value)
    { _meta[key] = value; return this; }

    public ApiResponseBuilder<T> WithWarning(string warning)
    { _warnings.Add(warning); return this; }

    public ApiResponseBuilder<T> WithError(string code, string message, string? field = null)
    { _errors.Add(new ApiError { Code = code, Message = message, Field = field }); return this; }

    public ApiResponseBuilder<T> WithDebug(ApiDebug debug)
    { _debug = debug; return this; }

    // ── Terminal ───────────────────────────────────────────────────────────────

    public ApiResponse<T> Build() => new()
    {
        Success  = _success,
        Message  = _message,
        Data     = _data,
        Meta     = _meta,
        Warnings = _warnings,
        Errors   = _errors,
        Debug    = _debug
    };
}

/// <summary>
/// Non-generic convenience factory — use when <c>Data</c> is always null (error responses).
/// </summary>
public static class ApiResponseBuilder
{
    /// <summary>Quick success with data.</summary>
    public static ApiResponse<T> Ok<T>(T data, string message = "Request processed successfully.")
        => ApiResponseBuilder<T>.Success(data).WithMessage(message).Build();

    /// <summary>Quick success with no data payload.</summary>
    public static ApiResponse<object?> Ok(string message = "Request processed successfully.")
        => ApiResponseBuilder<object?>.Success(null).WithMessage(message).Build();

    /// <summary>Quick single-error failure.</summary>
    public static ApiResponse<object?> Error(string code, string message, string? field = null)
        => ApiResponseBuilder<object?>.Fail()
               .WithMessage(message)
               .WithError(code, message, field)
               .Build();

    /// <summary>Quick validation failure from ModelState errors.</summary>
    public static ApiResponse<object?> ValidationError(
        IEnumerable<(string Field, string Message)> fieldErrors)
    {
        var builder = ApiResponseBuilder<object?>.Fail()
            .WithMessage("One or more validation errors occurred.");
        foreach (var (field, msg) in fieldErrors)
            builder.WithError(ErrorCodes.ValidationFailed, msg, field);
        return builder.Build();
    }
}

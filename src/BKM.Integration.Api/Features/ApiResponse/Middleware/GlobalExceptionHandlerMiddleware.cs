using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using BKM.Integration.Application.Features.ApiResponse.Builders;
using BKM.Integration.Application.Features.ApiResponse.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BKM.Integration.Api.Features.ApiResponse.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Catches every unhandled exception thrown anywhere in the pipeline and
/// converts it into a well-formed <see cref="ApiResponse{T}"/> JSON response.
///
/// Registration order in Program.cs:
///   app.UseGlobalExceptionHandler();   ← must be FIRST in the pipeline
///   app.UseHttpsRedirection();
///   app.MapControllers();
///
/// Exception → HTTP status mapping:
///   ArgumentException / ArgumentNullException    → 400
///   UnauthorizedAccessException                  → 401
///   KeyNotFoundException / FileNotFoundException → 404
///   InvalidOperationException                    → 409 Conflict
///   CryptographicException                       → 400 (tampered data)
///   NotImplementedException                      → 501
///   OperationCanceledException                   → 499 (client closed)
///   Everything else                              → 500
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment env)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = false
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, code, message) = MapException(ex);

        logger.LogError(ex,
            "Unhandled exception [{Code}] on {Method} {Path} — {Message}",
            code, context.Request.Method, context.Request.Path, ex.Message);

        var traceId = context.TraceIdentifier;

        // Debug block — only in non-Production
        ApiDebug? debug = env.IsProduction() ? null : new ApiDebug
        {
            TraceId       = traceId,
            ExceptionType = ex.GetType().FullName,
            StackTrace    = ex.StackTrace,
            Timestamp     = DateTimeOffset.UtcNow
        };

        var response = ApiResponseBuilder<object?>.Fail()
            .WithMessage(message)
            .WithError(code, message)
            .WithMeta("traceId", traceId)
            .WithDebug(debug!)
            .Build();

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)status;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOpts));
    }

    private static (HttpStatusCode status, string code, string message) MapException(Exception ex)
        => ex switch
        {
            ArgumentNullException e      => (HttpStatusCode.BadRequest,          ErrorCodes.BadRequest,          e.Message),
            ArgumentException e          => (HttpStatusCode.BadRequest,          ErrorCodes.BadRequest,          e.Message),
            CryptographicException       => (HttpStatusCode.BadRequest,          ErrorCodes.DecryptionFailed,    "The cipher package is invalid or has been tampered with."),
            UnauthorizedAccessException  => (HttpStatusCode.Unauthorized,        ErrorCodes.Unauthorized,        "You are not authorised to perform this action."),
            KeyNotFoundException e       => (HttpStatusCode.NotFound,            ErrorCodes.NotFound,            e.Message),
            FileNotFoundException e      => (HttpStatusCode.NotFound,            ErrorCodes.NotFound,            e.Message),
            InvalidOperationException e  => (HttpStatusCode.Conflict,            ErrorCodes.Conflict,            e.Message),
            NotImplementedException      => (HttpStatusCode.NotImplemented,      ErrorCodes.InternalError,       "This feature is not yet implemented."),
            OperationCanceledException   => ((HttpStatusCode)499,                ErrorCodes.Timeout,             "The request was cancelled."),
            TimeoutException             => (HttpStatusCode.GatewayTimeout,      ErrorCodes.Timeout,             "The operation timed out."),
            _                            => (HttpStatusCode.InternalServerError, ErrorCodes.InternalError,       "An unexpected error occurred. Please try again later.")
        };
}

/// <summary>Extension method for clean registration in Program.cs.</summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}

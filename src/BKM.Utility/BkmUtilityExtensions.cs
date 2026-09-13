using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Utility.Auth;
using BKM.Utility.Cache;
using BKM.Utility.Encryption;
using BKM.Utility.Email;
using BKM.Utility.Feed;
using BKM.Utility.UrlShortener;
using BKM.Utility.FileIngestion;
using BKM.Utility.AppLog;

namespace BKM.Utility;

/// <summary>
/// Entry point for consuming the BKM.Utility NuGet package.
///
/// ── Use all features (most common) ──────────────────────────────────────────
/// <code>
/// builder.Services.AddBkmUtility(builder.Configuration);
/// </code>
///
/// ── Use only specific features ───────────────────────────────────────────────
/// <code>
/// // Auth + Encryption only:
/// builder.Services
///     .AddBkmAuth(builder.Configuration)
///     .AddBkmEncryption(builder.Configuration);
///
/// // Email only (Encryption is pulled automatically as a required dependency):
/// builder.Services.AddBkmEmail(builder.Configuration);
///
/// // Feed only:
/// builder.Services.AddBkmFeed(builder.Configuration);
///
/// // URL Shortener only:
/// builder.Services.AddBkmUrlShortener(builder.Configuration);
///
/// // File Ingestion only:
/// builder.Services.AddBkmFileIngestion(builder.Configuration);
///
/// // App Logging only:
/// builder.Services.AddBkmAppLog(builder.Configuration);
/// </code>
/// </summary>
public static class BkmUtilityExtensions
{
    // ── All features at once ───────────────────────────────────────────────────

    /// <summary>
    /// Registers all BKM.Utility features.
    /// Equivalent to calling every individual AddBkm*() method.
    /// </summary>
    public static IServiceCollection AddBkmUtility(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddBkmCache(configuration)
            .AddBkmEncryption(configuration)
            .AddBkmAuth(configuration)
            .AddBkmEmail(configuration)
            .AddBkmFeed(configuration)
            .AddBkmUrlShortener(configuration)
            .AddBkmFileIngestion(configuration)
            .AddBkmAppLog(configuration);

        return services;
    }
}

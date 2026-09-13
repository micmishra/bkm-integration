using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Utility.Application.Features.AppLog.Interfaces;
using BKM.Utility.Application.Features.AppLog.Services;
using BKM.Utility.Domain.Features.AppLog.Interfaces;
using BKM.Utility.Infrastructure.Features.AppLog.Logging;
using BKM.Utility.Infrastructure.Features.AppLog.Purging;
using BKM.Utility.Infrastructure.Persistence;

namespace BKM.Utility.AppLog;

/// <summary>
/// Entry point for the BKM.Utility.AppLog NuGet package.
///
/// Usage:
/// <code>
/// // 1. Install:  dotnet add package BKM.Utility.AppLog
///
/// // 2. Wire up (Program.cs):
/// builder.Services.AddBkmAppLog(builder.Configuration);
///
/// // 3. Configure Serilog sink in Program.cs:
/// builder.Host.UseSerilog((ctx, svc, cfg) =>
///     SerilogConfigurator.Build(ctx.Configuration, svc).CreateLogger());
///
/// // 4. Configure (appsettings.json):
/// // "ConnectionStrings": { "AppLog": "...", "Cache": "..." },
/// // "Logging": { "MinimumLevel": "Information", "Sinks": { "Database": true, "File": true } }
///
/// // 5. Inject:
/// public class AuditHandler(IAppLogService logs) { ... }
/// </code>
///
/// Automatically includes: BKM.Utility.Cache.
/// Own database: AppLogDbContext → AppLogs, LogRetentionPolicies.
/// </summary>
public static class AppLogExtensions
{
    /// <summary>
    /// Registers structured logging (Serilog DB + file sinks), queryable log API,
    /// and per-feature retention policies with scheduled purge.
    /// Cache is registered automatically as a required dependency.
    /// </summary>
    public static IServiceCollection AddBkmAppLog(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        BKM.Utility.Cache.CacheExtensions.AddBkmCacheInfrastructure(services, configuration);
        services.AddBkmAppLogApplication();
        services.AddBkmAppLogInfrastructure(configuration);
        return services;
    }

    /// <summary>Registers the application-layer AppLog use-case services.</summary>
    public static IServiceCollection AddBkmAppLogApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IAppLogService,          AppLogService>();
        services.AddScoped<IRetentionPolicyService, RetentionPolicyService>();
        return services;
    }

    /// <summary>Registers the AppLog EF Core DbContext, repositories, and background purge service.</summary>
    public static IServiceCollection AddBkmAppLogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration, "AppLog");

        services.AddDbContext<AppLogDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IAppLogRepository,          AppLogRepository>();
        services.AddScoped<IRetentionPolicyRepository, RetentionPolicyRepository>();
        services.AddHostedService<LogPurgeBackgroundService>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, string featureKey)
        => configuration.GetConnectionString(featureKey)
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
               $"No connection string found for feature '{featureKey}' " +
               $"and no 'DefaultConnection' fallback is configured.");
}

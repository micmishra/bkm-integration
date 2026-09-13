using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Utility.Domain.Shared.Interfaces;
using BKM.Utility.Infrastructure.Persistence;
using BKM.Utility.Infrastructure.Shared.Caching;

namespace BKM.Utility.Cache;

/// <summary>
/// Entry point for the BKM.Utility.Cache NuGet package.
///
/// Usage:
/// <code>
/// // 1. Install:  dotnet add package BKM.Utility.Cache
///
/// // 2. Wire up (Program.cs):
/// builder.Services.AddBkmCache(builder.Configuration);
///
/// // 3. Configure (appsettings.json):
/// // "ConnectionStrings": { "Cache": "..." }   // or "DefaultConnection"
///
/// // 4. Inject anywhere:
/// public class MyService(ICacheService&lt;MyDto&gt; cache) { ... }
/// </code>
///
/// Own database: CacheDbContext → dbo.BkmCache.
/// No other BKM feature required.
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// Registers SQL Server distributed cache (ICacheService&lt;T&gt;, dbo.BkmCache).
    /// Connection string key: "Cache" (falls back to "DefaultConnection").
    /// </summary>
    public static IServiceCollection AddBkmCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBkmCacheInfrastructure(configuration);
        return services;
    }

    /// <summary>
    /// Registers the Cache infrastructure: CacheDbContext, IDistributedCache (SQL Server),
    /// and the open-generic ICacheService&lt;T&gt;.
    /// Called by dependent packages (Email, Feed, UrlShortener, AppLog).
    /// </summary>
    public static IServiceCollection AddBkmCacheInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration, "Cache");

        services.AddDbContext<CacheDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = connectionString;
            options.SchemaName       = "dbo";
            options.TableName        = "BkmCache";
        });

        // Open-generic: one registration covers ICacheService<T> for any T
        services.AddTransient(typeof(ICacheService<>), typeof(DistributedCacheService<>));

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, string featureKey)
        => configuration.GetConnectionString(featureKey)
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
               $"No connection string found for feature '{featureKey}' " +
               $"and no 'DefaultConnection' fallback is configured.");
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Utility.Application.Features.UrlShortener.Interfaces;
using BKM.Utility.Application.Features.UrlShortener.Services;
using BKM.Utility.Domain.Features.UrlShortener.Interfaces;
using BKM.Utility.Infrastructure.Features.UrlShortener.Persistence;
using BKM.Utility.Infrastructure.Persistence;

namespace BKM.Utility.UrlShortener;

/// <summary>
/// Entry point for the BKM.Utility.UrlShortener NuGet package.
///
/// Usage:
/// <code>
/// // 1. Install:  dotnet add package BKM.Utility.UrlShortener
///
/// // 2. Wire up (Program.cs):
/// builder.Services.AddBkmUrlShortener(builder.Configuration);
///
/// // 3. Configure (appsettings.json):
/// // "ConnectionStrings": { "UrlShortener": "...", "Cache": "..." },
/// // "BaseUrl": "https://short.example.com"
///
/// // 4. Inject:
/// public class ShortenerHandler(IUrlShortenerService shortener) { ... }
/// </code>
///
/// Automatically includes: BKM.Utility.Cache.
/// Own database: UrlShortenerDbContext → ShortenedUrls.
/// </summary>
public static class UrlShortenerExtensions
{
    /// <summary>
    /// Registers URL shortening and resolution with SQL Server distributed cache.
    /// Cache is registered automatically as a required dependency.
    /// </summary>
    public static IServiceCollection AddBkmUrlShortener(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        BKM.Utility.Cache.CacheExtensions.AddBkmCacheInfrastructure(services, configuration);
        services.AddBkmUrlShortenerApplication();
        services.AddBkmUrlShortenerInfrastructure(configuration);
        return services;
    }

    /// <summary>Registers the application-layer URL shortener use-case services.</summary>
    public static IServiceCollection AddBkmUrlShortenerApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IUrlShortenerService, UrlShortenerService>();
        return services;
    }

    /// <summary>Registers the UrlShortener EF Core DbContext and repositories.</summary>
    public static IServiceCollection AddBkmUrlShortenerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration, "UrlShortener");

        services.AddDbContext<UrlShortenerDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IUrlRepository, UrlRepository>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, string featureKey)
        => configuration.GetConnectionString(featureKey)
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
               $"No connection string found for feature '{featureKey}' " +
               $"and no 'DefaultConnection' fallback is configured.");
}

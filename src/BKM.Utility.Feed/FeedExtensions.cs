using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Utility.Application.Features.Feed.Interfaces;
using BKM.Utility.Application.Features.Feed.Services;
using BKM.Utility.Domain.Features.Feed.Interfaces;
using BKM.Utility.Infrastructure.Features.Feed.Persistence;
using BKM.Utility.Infrastructure.Persistence;

namespace BKM.Utility.Feed;

/// <summary>
/// Entry point for the BKM.Utility.Feed NuGet package.
///
/// Usage:
/// <code>
/// // 1. Install:  dotnet add package BKM.Utility.Feed
///
/// // 2. Wire up (Program.cs):
/// builder.Services.AddBkmFeed(builder.Configuration);
///
/// // 3. Configure (appsettings.json):
/// // "ConnectionStrings": { "Feed": "...", "Cache": "..." }
///
/// // 4. Inject:
/// public class TimelineHandler(IPullFeedService feed, IPostService posts) { ... }
/// </code>
///
/// Automatically includes: BKM.Utility.Cache.
/// Own database: FeedDbContext → Posts, Follows, UserFeeds.
/// </summary>
public static class FeedExtensions
{
    /// <summary>
    /// Registers Push/Pull feed, Posts, and Follow graph services.
    /// Cache is registered automatically as a required dependency.
    /// </summary>
    public static IServiceCollection AddBkmFeed(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        BKM.Utility.Cache.CacheExtensions.AddBkmCacheInfrastructure(services, configuration);
        services.AddBkmFeedApplication();
        services.AddBkmFeedInfrastructure(configuration);
        return services;
    }

    /// <summary>Registers the application-layer feed use-case services.</summary>
    public static IServiceCollection AddBkmFeedApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IPostService,      PostService>();
        services.AddScoped<IPushFeedService,  PushFeedService>();
        services.AddScoped<IPullFeedService,  PullFeedService>();
        services.AddScoped<IFollowService,    FollowService>();
        return services;
    }

    /// <summary>Registers the Feed EF Core DbContext and repositories.</summary>
    public static IServiceCollection AddBkmFeedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration, "Feed");

        services.AddDbContext<FeedDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IPostRepository,     PostRepository>();
        services.AddScoped<IFollowRepository,   FollowRepository>();
        services.AddScoped<IUserFeedRepository, UserFeedRepository>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, string featureKey)
        => configuration.GetConnectionString(featureKey)
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
               $"No connection string found for feature '{featureKey}' " +
               $"and no 'DefaultConnection' fallback is configured.");
}

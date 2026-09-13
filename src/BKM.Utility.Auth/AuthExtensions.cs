using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Utility.Application.Features.Auth.Interfaces;
using BKM.Utility.Application.Features.Auth.Services;
using BKM.Utility.Domain.Features.Auth.Entities;
using BKM.Utility.Domain.Features.Auth.Interfaces;
using BKM.Utility.Infrastructure.Features.Auth.Identity;
using BKM.Utility.Infrastructure.Features.Auth.Persistence;
using BKM.Utility.Infrastructure.Persistence;

namespace BKM.Utility.Auth;

/// <summary>
/// Entry point for the BKM.Utility.Auth NuGet package.
///
/// Usage:
/// <code>
/// // 1. Install:  dotnet add package BKM.Utility.Auth
///
/// // 2. Wire up (Program.cs):
/// builder.Services.AddBkmAuth(builder.Configuration);
///
/// // 3. Configure (appsettings.json):
/// // "ConnectionStrings": { "Auth": "..." },   // or "DefaultConnection"
/// // "Jwt": { "SecretKey": "...", "Issuer": "...", "Audience": "..." }
///
/// // 4. Inject anywhere:
/// public class LoginHandler(IAuthService auth) { ... }
/// </code>
///
/// Own database: AuthDbContext → migrated automatically on startup via Database.Migrate().
/// Owns tables: AspNetUsers, AspNetRoles, RefreshTokens, Permissions, RolePermissions.
/// </summary>
public static class AuthExtensions
{
    /// <summary>
    /// Registers JWT auth, ASP.NET Identity, refresh tokens, SSO, RBAC, and ABAC services.
    /// </summary>
    public static IServiceCollection AddBkmAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBkmAuthApplication();
        services.AddBkmAuthInfrastructure(configuration);
        return services;
    }

    /// <summary>Registers the application-layer auth use-case services.</summary>
    public static IServiceCollection AddBkmAuthApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService,           AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IPermissionService,     PermissionService>();
        return services;
    }

    /// <summary>Registers Identity, EF Core AuthDbContext, and repository implementations.</summary>
    public static IServiceCollection AddBkmAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration, "Auth");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequiredLength         = 8;
            options.Password.RequireDigit           = true;
            options.Password.RequireLowercase       = true;
            options.Password.RequireUppercase       = false;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail         = true;
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<ITokenService,           TokenService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPermissionRepository,   PermissionRepository>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, string featureKey)
        => configuration.GetConnectionString(featureKey)
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
               $"No connection string found for feature '{featureKey}' " +
               $"and no 'DefaultConnection' fallback is configured.");
}

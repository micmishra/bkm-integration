using Microsoft.Extensions.DependencyInjection;
using BKM.Integration.Application.Features.AppLog.Interfaces;
using BKM.Integration.Application.Features.FileIngestion.Interfaces;
using BKM.Integration.Application.Features.FileIngestion.Services;
using BKM.Integration.Application.Features.AppLog.Services;
using BKM.Integration.Application.Features.Auth.Interfaces;
using BKM.Integration.Application.Features.Auth.Services;
using BKM.Integration.Application.Features.Email.Interfaces;
using BKM.Integration.Application.Features.Email.Services;
using BKM.Integration.Application.Features.Encryption.Interfaces;
using BKM.Integration.Application.Features.Encryption.Services;
using BKM.Integration.Application.Features.Feed.Interfaces;
using BKM.Integration.Application.Features.Feed.Services;
using BKM.Integration.Application.Features.UrlShortener.Interfaces;
using BKM.Integration.Application.Features.UrlShortener.Services;

namespace BKM.Integration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ── UrlShortener feature ───────────────────────────────────────────────
        services.AddScoped<IUrlShortenerService, UrlShortenerService>();

        // ── Encryption feature ─────────────────────────────────────────────────
        services.AddScoped<IEncryptionAppService, EncryptionAppService>();

        // ── AppLog feature ─────────────────────────────────────────────────────
        services.AddScoped<IAppLogService, AppLogService>();
        services.AddScoped<IRetentionPolicyService, RetentionPolicyService>();

        // ── Email feature ──────────────────────────────────────────────────────
        services.AddScoped<IEmailService, EmailService>();

        // ── Feed feature ──────────────────────────────────────────────────────
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IPushFeedService, PushFeedService>();
        services.AddScoped<IPullFeedService, PullFeedService>();
        services.AddScoped<IFollowService, FollowService>();

        // ── Auth feature ──────────────────────────────────────────────────────
        services.AddScoped<IAuthService,           AuthService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        services.AddScoped<IPermissionService,     PermissionService>();

        // ── FileIngestion feature ──────────────────────────────────────────────
        services.AddScoped<IFileIngestionService, FileIngestionService>();

        return services;
    }
}

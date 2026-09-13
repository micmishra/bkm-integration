using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Integration.Application.Features.Auth.Interfaces;
using BKM.Integration.Application.Features.FileIngestion.Interfaces;
using BKM.Integration.Domain.Features.FileIngestion.Interfaces;
using BKM.Integration.Infrastructure.Features.FileIngestion.Parsing;
using BKM.Integration.Infrastructure.Features.FileIngestion.Persistence;
using BKM.Integration.Application.Features.Email.Interfaces;
using BKM.Integration.Domain.Features.AppLog.Interfaces;
using BKM.Integration.Domain.Features.Auth.Entities;
using BKM.Integration.Domain.Features.Auth.Interfaces;
using BKM.Integration.Domain.Features.Email.Interfaces;
using BKM.Integration.Domain.Features.Encryption.Interfaces;
using BKM.Integration.Domain.Features.Feed.Interfaces;
using BKM.Integration.Domain.Features.UrlShortener.Interfaces;
using BKM.Integration.Domain.Shared.Interfaces;
using BKM.Integration.Infrastructure.Features.AppLog.Logging;
using BKM.Integration.Infrastructure.Features.AppLog.Purging;
using BKM.Integration.Infrastructure.Features.Auth.Identity;
using BKM.Integration.Infrastructure.Features.Auth.Persistence;
using BKM.Integration.Infrastructure.Features.Email.Persistence;
using BKM.Integration.Infrastructure.Features.Email.Smtp;
using BKM.Integration.Infrastructure.Features.Encryption.Crypto;
using BKM.Integration.Infrastructure.Features.Feed.Persistence;
using BKM.Integration.Infrastructure.Features.UrlShortener.Persistence;
using BKM.Integration.Infrastructure.Persistence;
using BKM.Integration.Infrastructure.Shared.Caching;

namespace BKM.Integration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // ── Shared: EF Core DbContext ──────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        // ── Shared: SQL Server Distributed Cache ───────────────────────────────
        // Table schema is defined in AppDbContext and created by EF migrations.
        services.AddDistributedSqlServerCache(options =>
        {
            options.ConnectionString = connectionString;
            options.SchemaName       = "dbo";
            options.TableName        = "UrlCache";
        });

        // ── Shared: generic distributed cache (covers all features) ───────────
        // Open-generic: any feature injects ICacheService<T> — one registration covers all T.
        services.AddTransient(typeof(ICacheService<>), typeof(DistributedCacheService<>));

        // ── UrlShortener feature ───────────────────────────────────────────────
        services.AddScoped<IUrlRepository, UrlRepository>();

        // ── Encryption feature ─────────────────────────────────────────────────
        // AesGcmEncryptionService is stateless after construction — Singleton is safe and efficient.
        services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();

        // ── AppLog feature ─────────────────────────────────────────────────────
        services.AddScoped<IAppLogRepository, AppLogRepository>();
        services.AddScoped<IRetentionPolicyRepository, RetentionPolicyRepository>();
        services.AddHostedService<LogPurgeBackgroundService>();

        // ── Email feature ──────────────────────────────────────────────────────
        services.AddScoped<IEmailConfigRepository,   EmailConfigRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailAuditLogRepository, EmailAuditLogRepository>();
        // SmtpSender is transient — creates a new TCP connection per send
        services.AddTransient<ISmtpSender, SmtpSender>();

        // ── Feed feature ──────────────────────────────────────────────────────
        services.AddScoped<IPostRepository,     PostRepository>();
        services.AddScoped<IFollowRepository,   FollowRepository>();
        services.AddScoped<IUserFeedRepository, UserFeedRepository>();

        // ── Auth feature ──────────────────────────────────────────────────────
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
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<ITokenService,           TokenService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPermissionRepository,   PermissionRepository>();

        // ── FileIngestion feature ──────────────────────────────────────────────
        services.AddScoped<IIngestionRepository, IngestionRepository>();
        services.AddTransient<DelimitedFileParser>();
        services.AddTransient<JsonFileParser>();
        services.AddTransient<XmlFileParser>();
        services.AddTransient<ExcelFileParser>();
        services.AddTransient<IFileParserFactory, FileParserFactory>();

        return services;
    }
}

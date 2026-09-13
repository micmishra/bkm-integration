using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Utility.Application.Features.Email.Interfaces;
using BKM.Utility.Application.Features.Email.Services;
using BKM.Utility.Domain.Features.Email.Interfaces;
using BKM.Utility.Infrastructure.Features.Email.Persistence;
using BKM.Utility.Infrastructure.Features.Email.Smtp;
using BKM.Utility.Infrastructure.Persistence;

namespace BKM.Utility.Email;

/// <summary>
/// Entry point for the BKM.Utility.Email NuGet package.
///
/// Usage:
/// <code>
/// // 1. Install:  dotnet add package BKM.Utility.Email
///
/// // 2. Wire up (Program.cs):
/// builder.Services.AddBkmEmail(builder.Configuration);
///
/// // 3. Configure (appsettings.json):
/// // "ConnectionStrings": { "Email": "...", "Cache": "..." },
/// // "Encryption": { "Key": "&lt;32-byte Base64 key&gt;" }
///
/// // 4. Inject:
/// public class NotificationService(IEmailService email) { ... }
/// </code>
///
/// Automatically includes: BKM.Utility.Encryption + BKM.Utility.Cache.
/// Own database: EmailDbContext → EmailConfigs, EmailTemplates, EmailAuditLogs.
/// </summary>
public static class EmailExtensions
{
    /// <summary>
    /// Registers SMTP email with DB-stored configs, templates, audit log, and encrypted passwords.
    /// Encryption and Cache are registered automatically as required dependencies.
    /// </summary>
    public static IServiceCollection AddBkmEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Encryption is a hard dependency of EmailService (SMTP password decryption)
        BKM.Utility.Encryption.EncryptionExtensions.AddBkmEncryption(services, configuration);
        // Cache is used for config + template TTL
        BKM.Utility.Cache.CacheExtensions.AddBkmCacheInfrastructure(services, configuration);

        services.AddBkmEmailApplication();
        services.AddBkmEmailInfrastructure(configuration);
        return services;
    }

    /// <summary>Registers the application-layer email use-case services.</summary>
    public static IServiceCollection AddBkmEmailApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();
        return services;
    }

    /// <summary>Registers the Email EF Core DbContext and repositories.</summary>
    public static IServiceCollection AddBkmEmailInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration, "Email");

        services.AddDbContext<EmailDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IEmailConfigRepository,   EmailConfigRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailAuditLogRepository, EmailAuditLogRepository>();
        // SmtpSender is transient — creates a new TCP connection per send
        services.AddTransient<ISmtpSender, SmtpSender>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, string featureKey)
        => configuration.GetConnectionString(featureKey)
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
               $"No connection string found for feature '{featureKey}' " +
               $"and no 'DefaultConnection' fallback is configured.");
}

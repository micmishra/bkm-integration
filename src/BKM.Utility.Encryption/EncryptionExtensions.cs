using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Utility.Application.Features.Encryption.Interfaces;
using BKM.Utility.Application.Features.Encryption.Services;
using BKM.Utility.Domain.Features.Encryption.Interfaces;
using BKM.Utility.Infrastructure.Features.Encryption.Crypto;

namespace BKM.Utility.Encryption;

/// <summary>
/// Entry point for the BKM.Utility.Encryption NuGet package.
///
/// Usage:
/// <code>
/// // 1. Install:  dotnet add package BKM.Utility.Encryption
///
/// // 2. Wire up (Program.cs):
/// builder.Services.AddBkmEncryption(builder.Configuration);
///
/// // 3. Configure (appsettings.json):
/// // "Encryption": { "Key": "&lt;32-byte Base64 key&gt;" }
///
/// // 4. Inject anywhere:
/// public class MyService(IEncryptionAppService enc) { ... }
/// </code>
///
/// Zero database dependencies. No EF migrations required.
/// </summary>
public static class EncryptionExtensions
{
    /// <summary>
    /// Registers AES-256-GCM encryption services.
    /// Requires <c>Encryption:Key</c> (32-byte Base64) in configuration.
    /// </summary>
    public static IServiceCollection AddBkmEncryption(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBkmEncryptionApplication();
        services.AddBkmEncryptionInfrastructure(configuration);
        return services;
    }

    /// <summary>Registers the application-layer encryption use-case service.</summary>
    public static IServiceCollection AddBkmEncryptionApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IEncryptionAppService, EncryptionAppService>();
        return services;
    }

    /// <summary>Registers the AES-256-GCM infrastructure implementation.</summary>
    public static IServiceCollection AddBkmEncryptionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Stateless after construction — Singleton is safe and efficient
        services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
        return services;
    }
}

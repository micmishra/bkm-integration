using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BKM.Utility.Application.Features.FileIngestion.Interfaces;
using BKM.Utility.Application.Features.FileIngestion.Services;
using BKM.Utility.Domain.Features.FileIngestion.Interfaces;
using BKM.Utility.Infrastructure.Features.FileIngestion.Parsing;
using BKM.Utility.Infrastructure.Features.FileIngestion.Persistence;
using BKM.Utility.Infrastructure.Persistence;

namespace BKM.Utility.FileIngestion;

/// <summary>
/// Entry point for the BKM.Utility.FileIngestion NuGet package.
///
/// Usage:
/// <code>
/// // 1. Install:  dotnet add package BKM.Utility.FileIngestion
///
/// // 2. Wire up (Program.cs):
/// builder.Services.AddBkmFileIngestion(builder.Configuration);
///
/// // 3. Configure (appsettings.json):
/// // "ConnectionStrings": { "FileIngestion": "..." }   // or "DefaultConnection"
///
/// // 4. Inject:
/// public class ImportHandler(IFileIngestionService ingestion) { ... }
/// </code>
///
/// No cache dependency. Own database: FileIngestionDbContext → IngestionBatches, IngestedRecords.
/// Supports CSV, TSV, fixed-width, JSON, JSON Lines, XML, and Excel (.xlsx / .xls).
/// </summary>
public static class FileIngestionExtensions
{
    /// <summary>
    /// Registers file ingestion services and parsers (CSV/TSV/JSON/XML/Excel).
    /// No cache or other BKM feature required.
    /// </summary>
    public static IServiceCollection AddBkmFileIngestion(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBkmFileIngestionApplication();
        services.AddBkmFileIngestionInfrastructure(configuration);
        return services;
    }

    /// <summary>Registers the application-layer file ingestion use-case services.</summary>
    public static IServiceCollection AddBkmFileIngestionApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IFileIngestionService, FileIngestionService>();
        return services;
    }

    /// <summary>Registers the FileIngestion EF Core DbContext, repository, and parsers.</summary>
    public static IServiceCollection AddBkmFileIngestionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration, "FileIngestion");

        services.AddDbContext<FileIngestionDbContext>(options =>
            options.UseSqlServer(connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IIngestionRepository, IngestionRepository>();
        services.AddTransient<DelimitedFileParser>();
        services.AddTransient<JsonFileParser>();
        services.AddTransient<XmlFileParser>();
        services.AddTransient<ExcelFileParser>();
        services.AddTransient<IFileParserFactory, FileParserFactory>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, string featureKey)
        => configuration.GetConnectionString(featureKey)
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
               $"No connection string found for feature '{featureKey}' " +
               $"and no 'DefaultConnection' fallback is configured.");
}

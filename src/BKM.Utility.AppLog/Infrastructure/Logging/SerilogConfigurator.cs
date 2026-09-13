using BKM.Utility.Infrastructure.Features.AppLog.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace BKM.Utility.Infrastructure.Features.AppLog.Logging;

/// <summary>
/// Configures and builds the Serilog logger based on appsettings.json → Logging section.
///
/// Sinks enabled/disabled independently:
///   Logging:Sinks:Database  true/false
///   Logging:Sinks:File      true/false
///
/// Both sinks can be active simultaneously.
/// </summary>
public static class SerilogConfigurator
{
    /// <summary>
    /// Builds the Serilog logger configuration.
    /// Call this BEFORE WebApplication.CreateBuilder so bootstrap errors are captured.
    /// </summary>
    public static LoggerConfiguration Build(
        IConfiguration      config,
        IServiceProvider?   services = null)
    {
        var minLevel = Enum.TryParse<LogEventLevel>(
            config["Logging:MinimumLevel"] ?? "Information", out var lvl)
            ? lvl
            : LogEventLevel.Information;

        var dbEnabled   = config.GetValue<bool>("Logging:Sinks:Database", defaultValue: true);
        var fileEnabled = config.GetValue<bool>("Logging:Sinks:File",     defaultValue: true);
        var filePath    = config["Logging:File:Path"]       ?? "logs/bkm-.txt";
        var fileSize    = config.GetValue<long>("Logging:File:FileSizeLimitBytes", 10_485_760); // 10 MB
        var fileRetain  = config.GetValue<int>("Logging:File:RetainedFileCountLimit", 30);

        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .MinimumLevel.Override("Microsoft",            LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System",               LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}");

        if (fileEnabled)
        {
            logConfig.WriteTo.File(
                path:                   filePath,
                rollingInterval:        RollingInterval.Day,
                fileSizeLimitBytes:     fileSize,
                retainedFileCountLimit: fileRetain,
                rollOnFileSizeLimit:    true,
                shared:                 false,
                outputTemplate:         "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {TraceId} {Feature} {Message:lj}{NewLine}{Exception}");
        }

        if (dbEnabled && services is not null)
        {
            logConfig.WriteTo.Sink(new DatabaseLogSink(services));
        }

        return logConfig;
    }
}

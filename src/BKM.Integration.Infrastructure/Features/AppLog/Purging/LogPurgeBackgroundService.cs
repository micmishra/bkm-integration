using BKM.Integration.Domain.Features.AppLog.Entities;
using BKM.Integration.Domain.Features.AppLog.Interfaces;
using BKM.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BKM.Integration.Infrastructure.Features.AppLog.Purging;

/// <summary>
/// Background service that runs on a configurable schedule and purges:
///   1. DB rows in dbo.AppLogs older than the effective retention for each feature
///   2. Rolled log files on disk older than the effective file retention
///
/// Schedule: configurable via appsettings.json → Logging:Purge:RunIntervalHours (default: 24)
/// First run: delayed by Logging:Purge:InitialDelayMinutes (default: 5 min after startup)
///
/// Per-feature logic:
///   - Loads all LogRetentionPolicies from DB
///   - Groups AppLogs by Feature
///   - For each feature: applies the feature-specific policy or falls back to the default policy
///   - Features with RetentionDays = 0 are skipped (keep forever)
///
/// File purging:
///   - Scans the configured log file directory
///   - Deletes files whose last-write time exceeds the default policy's FileRetentionDays
///   - (File purge uses the default policy since files are not split per feature)
/// </summary>
public sealed class LogPurgeBackgroundService(
    IServiceProvider   services,
    IConfiguration     config,
    ILogger<LogPurgeBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var initialDelay  = TimeSpan.FromMinutes(config.GetValue<int>("Logging:Purge:InitialDelayMinutes", 5));
        var runInterval   = TimeSpan.FromHours(config.GetValue<int>("Logging:Purge:RunIntervalHours", 24));

        logger.LogInformation(
            "LogPurgeBackgroundService starting. First run in {Delay}, then every {Interval}.",
            initialDelay, runInterval);

        await Task.Delay(initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunPurgeAsync(stoppingToken);
            await Task.Delay(runInterval, stoppingToken);
        }
    }

    private async Task RunPurgeAsync(CancellationToken ct)
    {
        logger.LogInformation("Log purge started.");

        try
        {
            using var scope      = services.CreateScope();
            var db               = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var policyRepo       = scope.ServiceProvider.GetRequiredService<IRetentionPolicyRepository>();

            var policies         = await policyRepo.GetAllAsync(ct);
            var defaultPolicy    = policies.FirstOrDefault(p => p.Feature is null);
            var featurePolicies  = policies.Where(p => p.Feature is not null)
                                           .ToDictionary(p => p.Feature!, StringComparer.OrdinalIgnoreCase);

            var now = DateTime.UtcNow;

            // ── DB purge ──────────────────────────────────────────────────────
            // Get all distinct features currently in the log table
            var features = await db.AppLogs
                .Select(x => x.Feature)
                .Distinct()
                .ToListAsync(ct);

            // Also purge entries with null feature using default policy
            if (!features.Contains(null))
                features.Add(null);

            long totalDeleted = 0;

            foreach (var feature in features)
            {
                // Resolve effective policy
                LogRetentionPolicy? policy = feature is not null && featurePolicies.TryGetValue(feature, out var fp)
                    ? fp
                    : defaultPolicy;

                if (policy is null || policy.DbRetentionDays == 0) continue;

                var cutoff = now.AddDays(-policy.DbRetentionDays);

                var deleted = await db.AppLogs
                    .Where(x => x.Feature == feature && x.Timestamp < cutoff)
                    .ExecuteDeleteAsync(ct);

                totalDeleted += deleted;

                if (deleted > 0)
                    logger.LogInformation(
                        "DB purge: deleted {Count} rows for feature '{Feature}' (cutoff {Cutoff:yyyy-MM-dd}).",
                        deleted, feature ?? "(default)", cutoff);

                // Update purge stats on the matched policy
                if (policy.Id > 0)
                    await policyRepo.UpdatePurgeStatsAsync(policy.Id, now, deleted, ct);
            }

            logger.LogInformation("DB purge complete. Total deleted: {Total}.", totalDeleted);

            // ── File purge ────────────────────────────────────────────────────
            await PurgeLogFilesAsync(defaultPolicy, now, ct);
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "Log purge failed.");
        }
    }

    private Task PurgeLogFilesAsync(
        LogRetentionPolicy? defaultPolicy,
        DateTime now,
        CancellationToken ct)
    {
        if (defaultPolicy is null || defaultPolicy.FileRetentionDays == 0)
            return Task.CompletedTask;

        var logDir  = config["Logging:File:Path"] ?? "logs/bkm-.txt";
        var dirPath = Path.GetDirectoryName(Path.GetFullPath(logDir)) ?? "logs";

        if (!Directory.Exists(dirPath))
            return Task.CompletedTask;

        var cutoff  = now.AddDays(-defaultPolicy.FileRetentionDays);
        var deleted = 0;

        foreach (var file in Directory.EnumerateFiles(dirPath, "*.txt"))
        {
            try
            {
                var lastWrite = File.GetLastWriteTimeUtc(file);
                if (lastWrite < cutoff)
                {
                    File.Delete(file);
                    deleted++;
                    logger.LogInformation("File purge: deleted '{File}' (last write {LastWrite:yyyy-MM-dd}).",
                        file, lastWrite);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "File purge: could not delete '{File}'.", file);
            }
        }

        if (deleted > 0)
            logger.LogInformation("File purge complete. Deleted {Count} log files.", deleted);

        return Task.CompletedTask;
    }
}

using System.Text.Json;
using BKM.Integration.Domain.Features.AppLog.Entities;
using BKM.Integration.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace BKM.Integration.Infrastructure.Features.AppLog.Logging;

/// <summary>
/// Non-blocking, batch-writing Serilog sink for SQL Server.
///
/// ── How it avoids blocking your request threads ────────────────────────────
/// Emit() does three things:
///   1. Builds the AppLogEntry (pure in-memory, ~microseconds)
///   2. Calls TryAdd() on a lock-free BlockingCollection — returns immediately
///   3. Returns — the calling request thread is NEVER touched again
///
/// A single background thread drains the queue in configurable batches
/// (default 100 entries per SaveChanges). This means:
///   - 1 DB round-trip per 100 log entries instead of 1 per entry
///   - 100x fewer DB connections under high load
///
/// ── What happens when the queue is full ───────────────────────────────────
/// Queue capacity: 100,000 entries (configurable).
/// If the DB is too slow to keep up and the queue fills:
///   - New entries are dropped (never block the request thread)
///   - A dropped-entry counter is tracked
///   - The next successful flush logs a warning with the drop count
///
/// ── Shutdown safety ───────────────────────────────────────────────────────
/// On app shutdown, CompleteAdding() signals the flush thread to drain
/// remaining queue entries before the process exits (up to 10s grace period).
/// </summary>
public sealed class DatabaseLogSink : ILogEventSink, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly int              _batchSize;

    private readonly System.Collections.Concurrent.BlockingCollection<AppLogEntry> _queue;
    private readonly Thread  _flushThread;
    private volatile bool    _disposed;
    private long             _droppedCount;   // entries silently dropped when queue full

    public DatabaseLogSink(IServiceProvider services, int queueCapacity = 100_000, int batchSize = 100)
    {
        _services  = services;
        _batchSize = batchSize;
        _queue     = new(boundedCapacity: queueCapacity);

        _flushThread = new Thread(FlushLoop)
        {
            IsBackground = true,
            Name         = "BKM.DbLogSink"
        };
        _flushThread.Start();
    }

    // ── Called by Serilog on every log event — MUST return fast ───────────────

    public void Emit(LogEvent logEvent)
    {
        if (_disposed) return;

        var entry = BuildEntry(logEvent);

        if (!_queue.TryAdd(entry))
        {
            // Queue full — drop silently, never block the caller
            System.Threading.Interlocked.Increment(ref _droppedCount);
        }
    }

    // ── Background flush loop — runs on dedicated thread ─────────────────────

    private void FlushLoop()
    {
        var batch = new List<AppLogEntry>(_batchSize);

        foreach (var entry in _queue.GetConsumingEnumerable())
        {
            batch.Add(entry);

            // Drain all available entries up to batch size (non-blocking)
            while (batch.Count < _batchSize && _queue.TryTake(out var next))
                batch.Add(next);

            // If we dropped entries, inject a warning into the batch
            var dropped = System.Threading.Interlocked.Exchange(ref _droppedCount, 0);
            if (dropped > 0)
            {
                batch.Add(new AppLogEntry
                {
                    Level     = "Warning",
                    Message   = $"[DatabaseLogSink] {dropped} log entries were dropped because the queue was full.",
                    Timestamp = DateTime.UtcNow
                });
            }

            WriteBatch(batch);
            batch.Clear();
        }

        // Drain any remaining entries on shutdown
        if (batch.Count > 0)
            WriteBatch(batch);
    }

    private void WriteBatch(List<AppLogEntry> batch)
    {
        if (batch.Count == 0) return;
        try
        {
            using var scope = (_services.GetService(typeof(IServiceScopeFactory))
                as IServiceScopeFactory)!.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AppLogs.AddRange(batch);        // ← single INSERT per batch, not per entry
            db.SaveChanges();
        }
        catch
        {
            // Never throw from a sink — absorb DB errors silently
        }
    }

    // ── Entry builder (pure in-memory, called on the request thread) ──────────

    private static AppLogEntry BuildEntry(LogEvent logEvent)
    {
        logEvent.Properties.TryGetValue("TraceId",         out var traceId);
        logEvent.Properties.TryGetValue("Feature",         out var feature);
        logEvent.Properties.TryGetValue("MachineName",     out var machine);
        logEvent.Properties.TryGetValue("EnvironmentName", out var env);

        var props = logEvent.Properties.ToDictionary(
            k => k.Key,
            v => v.Value.ToString());

        return new AppLogEntry
        {
            Level       = logEvent.Level.ToString(),
            Message     = logEvent.RenderMessage(),
            Exception   = logEvent.Exception?.ToString(),
            TraceId     = Clean(traceId),
            Feature     = Clean(feature),
            MachineName = Clean(machine),
            Environment = Clean(env),
            Properties  = JsonSerializer.Serialize(props),
            Timestamp   = logEvent.Timestamp.UtcDateTime
        };
    }

    private static string? Clean(LogEventPropertyValue? v)
        => v?.ToString().Trim('"');

    // ── Graceful shutdown ─────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _queue.CompleteAdding();
        _flushThread.Join(TimeSpan.FromSeconds(10));  // wait for final flush
        _queue.Dispose();
    }
}

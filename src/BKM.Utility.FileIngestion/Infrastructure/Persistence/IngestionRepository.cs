using BKM.Utility.Domain.Features.FileIngestion.Entities;
using BKM.Utility.Domain.Features.FileIngestion.Interfaces;
using BKM.Utility.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BKM.Utility.Infrastructure.Features.FileIngestion.Persistence;

public sealed class IngestionRepository(FileIngestionDbContext db) : IIngestionRepository
{
    public async Task SaveBatchAsync(IngestionBatch batch, CancellationToken ct = default)
    {
        db.IngestionBatches.Add(batch);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateBatchAsync(IngestionBatch batch, CancellationToken ct = default)
    {
        db.IngestionBatches.Update(batch);
        await db.SaveChangesAsync(ct);
    }

    public Task<IngestionBatch?> GetBatchAsync(Guid batchId, CancellationToken ct = default)
        => db.IngestionBatches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == batchId, ct);

    public Task<IReadOnlyList<IngestionBatch>> GetBatchesAsync(int page, int pageSize, CancellationToken ct = default)
        => db.IngestionBatches.AsNoTracking()
             .OrderByDescending(x => x.StartedAt)
             .Skip((page - 1) * pageSize).Take(pageSize)
             .ToListAsync(ct)
             .ContinueWith<IReadOnlyList<IngestionBatch>>(t => t.Result, ct);

    public Task<int> GetBatchCountAsync(CancellationToken ct = default)
        => db.IngestionBatches.CountAsync(ct);

    public async Task BulkInsertAsync(IReadOnlyList<IngestedRecord> records, CancellationToken ct = default)
    {
        // AddRange + SaveChanges is efficient for batches ≤ 5000 rows with EF Core batching.
        // For production with millions of rows, replace with SqlBulkCopy.
        db.IngestedRecords.AddRange(records);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();  // release EF tracking memory after each batch
    }

    public async Task<HashSet<string>> GetExistingHashesAsync(
        Guid batchId, IEnumerable<string> hashes, CancellationToken ct = default)
    {
        var hashList = hashes.ToList();
        var existing = await db.IngestedRecords
            .AsNoTracking()
            .Where(r => r.BatchId == batchId && hashList.Contains(r.PayloadHash))
            .Select(r => r.PayloadHash)
            .ToListAsync(ct);
        return [.. existing];
    }

    public Task<IReadOnlyList<IngestedRecord>> QueryRecordsAsync(
        Guid? batchId, string? fileType, string? recordType,
        string? payloadKey, string? payloadValue,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.IngestedRecords.AsNoTracking().AsQueryable();

        if (batchId.HasValue)
            q = q.Where(x => x.BatchId == batchId.Value);
        if (!string.IsNullOrWhiteSpace(fileType))
            q = q.Where(x => x.FileType == fileType);
        if (!string.IsNullOrWhiteSpace(recordType))
            q = q.Where(x => x.RecordType == recordType);
        if (!string.IsNullOrWhiteSpace(payloadKey) && !string.IsNullOrWhiteSpace(payloadValue))
        {
            // Use EF Core's EF.Functions.Like on the JSON string for portability.
            // The IX_IngestedRecords_Payload index in SQL Server supports JSON_VALUE queries.
            q = q.Where(x => EF.Functions.Like(x.Payload, $"%\"{payloadKey}\":\"{payloadValue}\"%"));
        }

        return q.OrderBy(x => x.BatchId).ThenBy(x => x.RowNumber)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct)
                .ContinueWith<IReadOnlyList<IngestedRecord>>(t => t.Result, ct);
    }

    public Task<int> CountRecordsAsync(
        Guid? batchId, string? fileType, string? recordType,
        string? payloadKey, string? payloadValue,
        CancellationToken ct = default)
    {
        var q = db.IngestedRecords.AsNoTracking().AsQueryable();
        if (batchId.HasValue) q = q.Where(x => x.BatchId == batchId.Value);
        if (!string.IsNullOrWhiteSpace(fileType)) q = q.Where(x => x.FileType == fileType);
        if (!string.IsNullOrWhiteSpace(recordType)) q = q.Where(x => x.RecordType == recordType);
        if (!string.IsNullOrWhiteSpace(payloadKey) && !string.IsNullOrWhiteSpace(payloadValue))
            q = q.Where(x => EF.Functions.Like(x.Payload, $"%\"{payloadKey}\":\"{payloadValue}\"%"));
        return q.CountAsync(ct);
    }
}

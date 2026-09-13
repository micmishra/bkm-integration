using BKM.Integration.Domain.Features.FileIngestion.Entities;

namespace BKM.Integration.Domain.Features.FileIngestion.Interfaces;

public interface IIngestionRepository
{
    Task SaveBatchAsync(IngestionBatch batch, CancellationToken ct = default);
    Task UpdateBatchAsync(IngestionBatch batch, CancellationToken ct = default);
    Task<IngestionBatch?> GetBatchAsync(Guid batchId, CancellationToken ct = default);
    Task<IReadOnlyList<IngestionBatch>> GetBatchesAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> GetBatchCountAsync(CancellationToken ct = default);

    /// <summary>Bulk-insert a batch of parsed records efficiently.</summary>
    Task BulkInsertAsync(IReadOnlyList<IngestedRecord> records, CancellationToken ct = default);

    /// <summary>Check which payload hashes already exist (for duplicate detection).</summary>
    Task<HashSet<string>> GetExistingHashesAsync(Guid batchId, IEnumerable<string> hashes, CancellationToken ct = default);

    Task<IReadOnlyList<IngestedRecord>> QueryRecordsAsync(
        Guid? batchId, string? fileType, string? recordType,
        string? payloadKey, string? payloadValue,
        int page, int pageSize, CancellationToken ct = default);

    Task<int> CountRecordsAsync(
        Guid? batchId, string? fileType, string? recordType,
        string? payloadKey, string? payloadValue,
        CancellationToken ct = default);
}

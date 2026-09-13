using BKM.Integration.Application.Features.FileIngestion.DTOs;

namespace BKM.Integration.Application.Features.FileIngestion.Interfaces;

public interface IFileIngestionService
{
    /// <summary>
    /// Streams a file from the given stream, parses it according to options,
    /// and writes records to DB in batches. Never loads the whole file into memory.
    /// Returns the batch summary.
    /// </summary>
    Task<IngestionBatchDto> IngestAsync(
        Stream          fileStream,
        string          fileName,
        IngestFileRequest options,
        CancellationToken ct = default);

    Task<IngestionBatchDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default);
    Task<(IReadOnlyList<IngestionBatchDto> Items, int Total)> GetBatchesAsync(int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<IngestedRecordDto> Items, int Total)> QueryRecordsAsync(
        QueryRecordsRequest request, CancellationToken ct = default);
}

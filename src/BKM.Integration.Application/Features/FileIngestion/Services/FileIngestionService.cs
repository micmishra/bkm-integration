using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BKM.Integration.Application.Features.FileIngestion.DTOs;
using BKM.Integration.Application.Features.FileIngestion.Interfaces;
using BKM.Integration.Domain.Features.FileIngestion.Entities;
using BKM.Integration.Domain.Features.FileIngestion.Interfaces;

namespace BKM.Integration.Application.Features.FileIngestion.Services;

public sealed class FileIngestionService(
    IIngestionRepository repository,
    IFileParserFactory   parserFactory) : IFileIngestionService
{
    public async Task<IngestionBatchDto> IngestAsync(
        Stream fileStream, string fileName,
        IngestFileRequest options, CancellationToken ct = default)
    {
        var parseOpts = BuildParseOptions(options);
        var fileType  = DetectFileType(fileName);

        var batch = new IngestionBatch
        {
            Id           = Guid.NewGuid(),
            FileName     = fileName,
            FileType     = fileType,
            RecordType   = options.RecordType,
            ParseOptions = JsonSerializer.Serialize(parseOpts),
            Status       = "Processing",
            StartedAt    = DateTime.UtcNow
        };
        await repository.SaveBatchAsync(batch, ct);

        try
        {
            var parser = parserFactory.GetParser(fileType);
            var buffer = new List<IngestedRecord>(parseOpts.BatchSize);
            long rowNum = 0;

            await foreach (var row in parser.ParseAsync(fileStream, fileName, parseOpts, ct))
            {
                rowNum++;
                batch.TotalRows++;

                var payload     = JsonSerializer.Serialize(row);
                var payloadHash = HashPayload(payload);

                buffer.Add(new IngestedRecord
                {
                    BatchId     = batch.Id,
                    FileName    = fileName,
                    FileType    = fileType,
                    RecordType  = options.RecordType,
                    RowNumber   = rowNum,
                    Payload     = payload,
                    PayloadHash = payloadHash,
                    IngestedAt  = DateTime.UtcNow
                });

                if (buffer.Count >= parseOpts.BatchSize)
                {
                    await FlushBufferAsync(buffer, batch, parseOpts.SkipDuplicates, ct);
                    buffer.Clear();
                }
            }

            // Flush remaining
            if (buffer.Count > 0)
                await FlushBufferAsync(buffer, batch, parseOpts.SkipDuplicates, ct);

            batch.Status      = "Completed";
            batch.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            batch.Status       = "Failed";
            batch.ErrorMessage = ex.Message;
            batch.CompletedAt  = DateTime.UtcNow;
        }

        await repository.UpdateBatchAsync(batch, ct);
        return MapBatch(batch);
    }

    public async Task<IngestionBatchDto?> GetBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await repository.GetBatchAsync(batchId, ct);
        return batch is null ? null : MapBatch(batch);
    }

    public async Task<(IReadOnlyList<IngestionBatchDto> Items, int Total)> GetBatchesAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var items = await repository.GetBatchesAsync(page, pageSize, ct);
        var total = await repository.GetBatchCountAsync(ct);
        return (items.Select(MapBatch).ToList(), total);
    }

    public async Task<(IReadOnlyList<IngestedRecordDto> Items, int Total)> QueryRecordsAsync(
        QueryRecordsRequest request, CancellationToken ct = default)
    {
        var page     = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 500);
        var items    = await repository.QueryRecordsAsync(
            request.BatchId, request.FileType, request.RecordType,
            request.PayloadKey, request.PayloadValue, page, pageSize, ct);
        var total    = await repository.CountRecordsAsync(
            request.BatchId, request.FileType, request.RecordType,
            request.PayloadKey, request.PayloadValue, ct);
        return (items.Select(MapRecord).ToList(), total);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task FlushBufferAsync(
        List<IngestedRecord> buffer, IngestionBatch batch,
        bool skipDuplicates, CancellationToken ct)
    {
        List<IngestedRecord> toInsert;

        if (skipDuplicates)
        {
            var hashes    = buffer.Select(r => r.PayloadHash);
            var existing  = await repository.GetExistingHashesAsync(batch.Id, hashes, ct);
            var dupeCount = 0;
            toInsert = buffer.Where(r =>
            {
                if (existing.Contains(r.PayloadHash)) { dupeCount++; return false; }
                return true;
            }).ToList();
            batch.DuplicateRows += dupeCount;
        }
        else
        {
            toInsert = buffer;
        }

        if (toInsert.Count > 0)
        {
            await repository.BulkInsertAsync(toInsert, ct);
            batch.SuccessRows += toInsert.Count;
        }
    }

    private static string HashPayload(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private static string DetectFileType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".csv" or ".txt" or ".tsv" or ".dat" or ".pipe" => "delimited",
            ".json" or ".jsonl" or ".ndjson"                 => "json",
            ".xml"                                           => "xml",
            ".xlsx" or ".xls"                                => "excel",
            _                                                => "delimited"  // fallback
        };
    }

    private static ParseOptions BuildParseOptions(IngestFileRequest req)
    {
        var opts = new ParseOptions
        {
            Delimiter      = req.Delimiter,
            HasHeader      = req.HasHeader,
            IsJsonLines    = req.IsJsonLines,
            XmlRecordXPath = req.XmlRecordXPath,
            Encoding       = req.Encoding,
            BatchSize      = Math.Clamp(req.BatchSize, 10, 5000),
            RecordType     = req.RecordType,
            SkipDuplicates = req.SkipDuplicates
        };

        if (!string.IsNullOrWhiteSpace(req.FixedWidthJson))
        {
            try
            {
                opts.FixedWidths = JsonSerializer.Deserialize<List<FixedWidthColumn>>(
                    req.FixedWidthJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? [];
            }
            catch { /* ignore malformed fixed-width spec */ }
        }

        return opts;
    }

    private static IngestionBatchDto MapBatch(IngestionBatch b) => new()
    {
        Id            = b.Id,
        FileName      = b.FileName,
        FileType      = b.FileType,
        RecordType    = b.RecordType,
        TotalRows     = b.TotalRows,
        SuccessRows   = b.SuccessRows,
        DuplicateRows = b.DuplicateRows,
        ErrorRows     = b.ErrorRows,
        Status        = b.Status,
        ErrorMessage  = b.ErrorMessage,
        StartedAt     = b.StartedAt,
        CompletedAt   = b.CompletedAt
    };

    private static IngestedRecordDto MapRecord(IngestedRecord r) => new()
    {
        Id         = r.Id,
        BatchId    = r.BatchId,
        FileName   = r.FileName,
        FileType   = r.FileType,
        RecordType = r.RecordType,
        RowNumber  = r.RowNumber,
        Payload    = r.Payload,
        IngestedAt = r.IngestedAt
    };
}

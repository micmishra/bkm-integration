using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.FileIngestion.DTOs;
using BKM.Utility.Application.Features.FileIngestion.Interfaces;

namespace BKM.Utility.Api.Features.FileIngestion;

[ApiController]
[Route("api/ingest")]
public sealed class FileIngestionController(IFileIngestionService fileIngestionService) : ControllerBase
{
    /// <summary>
    /// Upload a file for ingestion. Accepts multipart/form-data with the file and parse options.
    /// Streams the file without loading it fully into memory.
    /// </summary>
    /// <remarks>
    ///     POST /api/ingest/upload
    ///     Content-Type: multipart/form-data
    ///     Fields: file (required), recordType, delimiter, hasHeader, isJsonLines,
    ///             xmlRecordXPath, encoding, skipDuplicates, batchSize, fixedWidthJson
    /// </remarks>
    [HttpPost("upload")]
    [RequestSizeLimit(500_000_000)]   // 500 MB
    [IgnoreAntiforgeryToken]
    [ProducesResponseType<ApiResponse<IngestionBatchDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] IngestFileRequest options,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var result = await fileIngestionService.IngestAsync(stream, file.FileName, options, ct);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponseBuilder<IngestionBatchDto>
                .Success(result)
                .WithMessage($"File '{file.FileName}' ingested. {result.SuccessRows} rows written, {result.DuplicateRows} duplicates skipped.")
                .WithMeta("batchId",       result.Id)
                .WithMeta("status",        result.Status)
                .WithMeta("totalRows",     result.TotalRows)
                .WithMeta("successRows",   result.SuccessRows)
                .WithMeta("duplicateRows", result.DuplicateRows)
                .Build());
    }

    /// <summary>List all ingestion batches with pagination.</summary>
    /// <remarks>GET /api/ingest/batches?page=1&amp;pageSize=20</remarks>
    [HttpGet("batches")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<IngestionBatchDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBatches(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct     = default)
    {
        var clampedPage     = Math.Max(page, 1);
        var clampedPageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await fileIngestionService.GetBatchesAsync(clampedPage, clampedPageSize, ct);

        return Ok(ApiResponseBuilder<IReadOnlyList<IngestionBatchDto>>
            .Success(items)
            .WithMessage($"Retrieved {items.Count} ingestion batches.")
            .WithMeta("totalCount",  total)
            .WithMeta("page",        clampedPage)
            .WithMeta("pageSize",    clampedPageSize)
            .WithMeta("totalPages",  (int)Math.Ceiling(total / (double)clampedPageSize))
            .Build());
    }

    /// <summary>Get a specific ingestion batch by ID.</summary>
    /// <remarks>GET /api/ingest/batches/{id}</remarks>
    [HttpGet("batches/{id:guid}")]
    [ProducesResponseType<ApiResponse<IngestionBatchDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatch(Guid id, CancellationToken ct)
    {
        var batch = await fileIngestionService.GetBatchAsync(id, ct);
        if (batch is null)
            return NotFound(ApiResponseBuilder.Error("NotFound", $"Batch '{id}' not found."));

        return Ok(ApiResponseBuilder<IngestionBatchDto>
            .Success(batch)
            .WithMessage("Batch retrieved.")
            .Build());
    }

    /// <summary>
    /// Query ingested records with optional filters.
    /// Filter by batchId, fileType, recordType, or a specific JSON payload key/value.
    /// </summary>
    /// <remarks>
    ///     GET /api/ingest/records?batchId=...&amp;payloadKey=CustomerId&amp;payloadValue=42&amp;page=1&amp;pageSize=50
    /// </remarks>
    [HttpGet("records")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<IngestedRecordDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> QueryRecords(
        [FromQuery] QueryRecordsRequest request,
        CancellationToken ct)
    {
        var (items, total) = await fileIngestionService.QueryRecordsAsync(request, ct);
        var pageSize = Math.Clamp(request.PageSize, 1, 500);

        return Ok(ApiResponseBuilder<IReadOnlyList<IngestedRecordDto>>
            .Success(items)
            .WithMessage($"Retrieved {items.Count} records.")
            .WithMeta("totalCount",  total)
            .WithMeta("page",        Math.Max(request.Page, 1))
            .WithMeta("pageSize",    pageSize)
            .WithMeta("totalPages",  (int)Math.Ceiling(total / (double)pageSize))
            .Build());
    }
}

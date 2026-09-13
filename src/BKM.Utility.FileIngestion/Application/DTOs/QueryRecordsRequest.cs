namespace BKM.Utility.Application.Features.FileIngestion.DTOs;

public sealed class QueryRecordsRequest
{
    public Guid?   BatchId      { get; set; }
    public string? FileType     { get; set; }
    public string? RecordType   { get; set; }
    /// <summary>JSON key to filter on inside the Payload column.</summary>
    public string? PayloadKey   { get; set; }
    /// <summary>Value the PayloadKey must equal.</summary>
    public string? PayloadValue { get; set; }
    public int     Page         { get; set; } = 1;
    public int     PageSize     { get; set; } = 50;
}

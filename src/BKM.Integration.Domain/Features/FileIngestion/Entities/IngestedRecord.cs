namespace BKM.Integration.Domain.Features.FileIngestion.Entities;

public sealed class IngestedRecord
{
    public long     Id           { get; set; }
    public Guid     BatchId      { get; set; }           // groups all rows from one upload
    public string   FileName     { get; set; } = string.Empty;
    public string   FileType     { get; set; } = string.Empty;  // csv, tsv, json, xml, excel
    public string?  RecordType   { get; set; }           // caller-supplied label, e.g. "Customer"
    public long     RowNumber    { get; set; }           // 1-based row number within the file
    public string   Payload      { get; set; } = string.Empty;  // JSON object {"col":"val",...}
    public string   PayloadHash  { get; set; } = string.Empty;  // SHA-256(Payload) for dedup
    public DateTime IngestedAt   { get; set; }
}

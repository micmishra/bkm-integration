namespace BKM.Integration.Application.Features.FileIngestion.DTOs;

public sealed class IngestedRecordDto
{
    public long     Id          { get; init; }
    public Guid     BatchId     { get; init; }
    public string   FileName    { get; init; } = string.Empty;
    public string   FileType    { get; init; } = string.Empty;
    public string?  RecordType  { get; init; }
    public long     RowNumber   { get; init; }
    public string   Payload     { get; init; } = string.Empty; // raw JSON string
    public DateTime IngestedAt  { get; init; }
}

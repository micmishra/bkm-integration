namespace BKM.Integration.Application.Features.FileIngestion.DTOs;

public sealed class IngestionBatchDto
{
    public Guid     Id            { get; init; }
    public string   FileName      { get; init; } = string.Empty;
    public string   FileType      { get; init; } = string.Empty;
    public string?  RecordType    { get; init; }
    public long     TotalRows     { get; init; }
    public long     SuccessRows   { get; init; }
    public long     DuplicateRows { get; init; }
    public long     ErrorRows     { get; init; }
    public string   Status        { get; init; } = string.Empty;
    public string?  ErrorMessage  { get; init; }
    public DateTime StartedAt     { get; init; }
    public DateTime? CompletedAt  { get; init; }
}

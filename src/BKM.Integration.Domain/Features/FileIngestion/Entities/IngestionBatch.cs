namespace BKM.Integration.Domain.Features.FileIngestion.Entities;

public sealed class IngestionBatch
{
    public Guid     Id            { get; set; }
    public string   FileName      { get; set; } = string.Empty;
    public string   FileType      { get; set; } = string.Empty;
    public string?  RecordType    { get; set; }
    public string?  ParseOptions  { get; set; }  // JSON: delimiter, hasHeader, fixedWidths etc.
    public long     TotalRows     { get; set; }
    public long     SuccessRows   { get; set; }
    public long     DuplicateRows { get; set; }
    public long     ErrorRows     { get; set; }
    public string   Status        { get; set; } = "Pending";  // Pending, Processing, Completed, Failed
    public string?  ErrorMessage  { get; set; }
    public DateTime StartedAt     { get; set; }
    public DateTime? CompletedAt  { get; set; }
}

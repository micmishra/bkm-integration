namespace BKM.Utility.Application.Features.FileIngestion.DTOs;

public sealed class IngestFileRequest
{
    public string?  RecordType     { get; set; }
    public string   Delimiter      { get; set; } = ",";
    public bool     HasHeader      { get; set; } = true;
    public bool     IsJsonLines    { get; set; } = false;
    public string   XmlRecordXPath { get; set; } = "/*/*";
    public string   Encoding       { get; set; } = "utf-8";
    public bool     SkipDuplicates { get; set; } = true;
    public int      BatchSize      { get; set; } = 500;
    /// <summary>Fixed-width columns as JSON: [{"name":"Id","startIndex":0,"length":10},...]</summary>
    public string?  FixedWidthJson { get; set; }
}

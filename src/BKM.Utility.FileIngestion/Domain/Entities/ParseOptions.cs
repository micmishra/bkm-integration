namespace BKM.Utility.Domain.Features.FileIngestion.Entities;

public sealed class ParseOptions
{
    /// <summary>Delimiter for delimited text files. Default: comma. Use \t for TSV, | for pipe, etc.</summary>
    public string  Delimiter       { get; set; } = ",";

    /// <summary>Whether the first row contains column headers. Default: true.</summary>
    public bool    HasHeader       { get; set; } = true;

    /// <summary>
    /// Fixed-width column definitions. When non-empty, file is parsed as fixed-width
    /// regardless of Delimiter. Each entry = (ColumnName, StartIndex, Length).
    /// </summary>
    public List<FixedWidthColumn> FixedWidths { get; set; } = [];

    /// <summary>Encoding name. Default: utf-8.</summary>
    public string  Encoding        { get; set; } = "utf-8";

    /// <summary>XML: XPath to the repeating element. Default: /* (root children).</summary>
    public string  XmlRecordXPath  { get; set; } = "/*/*";

    /// <summary>JSON: whether input is JSON Lines (one object per line) vs JSON array.</summary>
    public bool    IsJsonLines      { get; set; } = false;

    /// <summary>Batch write size to DB. Default: 500.</summary>
    public int     BatchSize        { get; set; } = 500;

    /// <summary>Caller-supplied label for the record type, e.g. "Customer", "Invoice".</summary>
    public string? RecordType       { get; set; }

    /// <summary>If true, duplicate rows (same PayloadHash) are skipped silently.</summary>
    public bool    SkipDuplicates   { get; set; } = true;
}

public sealed class FixedWidthColumn
{
    public string Name       { get; set; } = string.Empty;
    public int    StartIndex { get; set; }
    public int    Length     { get; set; }
}

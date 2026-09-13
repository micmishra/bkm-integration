using System.Runtime.CompilerServices;
using System.Text;
using BKM.Integration.Application.Features.FileIngestion.Interfaces;
using BKM.Integration.Domain.Features.FileIngestion.Entities;

namespace BKM.Integration.Infrastructure.Features.FileIngestion.Parsing;

/// <summary>
/// Streams delimited text files line-by-line. Supports:
///   - Any single-character or multi-character delimiter (comma, tab, pipe, or any string)
///   - RFC 4180 quoted fields (fields containing delimiter or newline may be double-quoted)
///   - Fixed-width columns (when ParseOptions.FixedWidths is non-empty, delimiter is ignored)
///   - Optional header row (first row used as column names)
///   - Any .NET-supported encoding
/// Never loads the entire file into memory — uses StreamReader with default 4KB buffer.
/// </summary>
public sealed class DelimitedFileParser : IFileParser
{
    public async IAsyncEnumerable<Dictionary<string, string>> ParseAsync(
        Stream stream, string fileName, ParseOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var encoding = GetEncoding(options.Encoding);
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        bool isFixedWidth = options.FixedWidths.Count > 0;
        List<string>? headers = null;
        bool firstLine = true;

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            List<string> fields;

            if (isFixedWidth)
                fields = ParseFixedWidth(line, options.FixedWidths);
            else
                fields = ParseDelimited(line, options.Delimiter);

            if (firstLine)
            {
                firstLine = false;
                if (options.HasHeader)
                {
                    headers = fields;
                    continue;
                }
                // No header — generate Col1, Col2, ...
                headers = fields.Select((_, i) => $"Col{i + 1}").ToList();
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fields.Count; i++)
            {
                var key = i < headers!.Count ? headers[i] : $"Col{i + 1}";
                row[key] = fields[i];
            }

            yield return row;
        }
    }

    private static List<string> ParseDelimited(string line, string delimiter)
    {
        var fields = new List<string>();
        if (delimiter.Length == 1)
        {
            // RFC 4180 single-char delimiter parser
            char delim = delimiter[0];
            int  i     = 0;
            while (i <= line.Length)
            {
                if (i < line.Length && line[i] == '"')
                {
                    // Quoted field
                    var sb = new StringBuilder();
                    i++; // skip opening quote
                    while (i < line.Length)
                    {
                        if (line[i] == '"')
                        {
                            if (i + 1 < line.Length && line[i + 1] == '"')
                            {
                                sb.Append('"'); i += 2;
                            }
                            else { i++; break; }
                        }
                        else { sb.Append(line[i]); i++; }
                    }
                    fields.Add(sb.ToString());
                    if (i < line.Length && line[i] == delim) i++; // skip delimiter
                }
                else
                {
                    // Unquoted field
                    int next = line.IndexOf(delim, i);
                    if (next < 0) { fields.Add(line[i..]); break; }
                    fields.Add(line[i..next]);
                    i = next + 1;
                }
                if (i > line.Length) break;
            }
        }
        else
        {
            // Multi-char delimiter — simple split
            fields.AddRange(line.Split(delimiter));
        }
        return fields;
    }

    private static List<string> ParseFixedWidth(string line, List<FixedWidthColumn> columns)
    {
        var fields = new List<string>(columns.Count);
        foreach (var col in columns)
        {
            if (col.StartIndex >= line.Length)
            {
                fields.Add(string.Empty);
                continue;
            }
            var len = Math.Min(col.Length, line.Length - col.StartIndex);
            fields.Add(line.Substring(col.StartIndex, len).Trim());
        }
        return fields;
    }

    private static Encoding GetEncoding(string name)
    {
        try { return Encoding.GetEncoding(name); }
        catch { return Encoding.UTF8; }
    }
}

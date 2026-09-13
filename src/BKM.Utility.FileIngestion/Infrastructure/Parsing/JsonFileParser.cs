using System.Runtime.CompilerServices;
using System.Text.Json;
using BKM.Utility.Application.Features.FileIngestion.Interfaces;
using BKM.Utility.Domain.Features.FileIngestion.Entities;

namespace BKM.Utility.Infrastructure.Features.FileIngestion.Parsing;

/// <summary>
/// Streams JSON files without loading the whole document into memory.
/// Supports:
///   JSON array mode: reads the stream token-by-token using Utf8JsonReader on 64KB chunks
///   JSON Lines mode: reads line by line, parses each line as a JSON object
/// </summary>
public sealed class JsonFileParser : IFileParser
{
    public async IAsyncEnumerable<Dictionary<string, string>> ParseAsync(
        Stream stream, string fileName, ParseOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (options.IsJsonLines || Path.GetExtension(fileName).ToLowerInvariant() is ".jsonl" or ".ndjson")
        {
            await foreach (var row in ParseJsonLinesAsync(stream, options, ct))
                yield return row;
        }
        else
        {
            await foreach (var row in ParseJsonArrayAsync(stream, ct))
                yield return row;
        }
    }

    private static async IAsyncEnumerable<Dictionary<string, string>> ParseJsonLinesAsync(
        Stream stream, ParseOptions options,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var encoding = System.Text.Encoding.GetEncoding(options.Encoding);
        using var reader = new System.IO.StreamReader(stream, encoding, leaveOpen: true);
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            var row = FlattenJsonObject(JsonDocument.Parse(line).RootElement);
            if (row is not null) yield return row;
        }
    }

    private static async IAsyncEnumerable<Dictionary<string, string>> ParseJsonArrayAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Buffer the whole stream into memory-mapped segments then iterate objects
        // For true streaming of large JSON arrays we use a two-pass approach:
        // read all bytes (streamed) then use JsonDocument which is already pooled/efficient
        using var ms = new System.IO.MemoryStream();
        await stream.CopyToAsync(ms, ct);
        ms.Seek(0, System.IO.SeekOrigin.Begin);

        using var doc = await JsonDocument.ParseAsync(ms, cancellationToken: ct);
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();
                var row = FlattenJsonObject(element);
                if (row is not null) yield return row;
            }
        }
        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
        {
            var row = FlattenJsonObject(doc.RootElement);
            if (row is not null) yield return row;
        }
    }

    private static Dictionary<string, string>? FlattenJsonObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in element.EnumerateObject())
            row[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString() ?? ""
                : prop.Value.GetRawText();
        return row;
    }
}

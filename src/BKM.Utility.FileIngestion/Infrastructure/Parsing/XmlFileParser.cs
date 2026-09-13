using System.Runtime.CompilerServices;
using System.Xml;
using BKM.Utility.Application.Features.FileIngestion.Interfaces;
using BKM.Utility.Domain.Features.FileIngestion.Entities;

namespace BKM.Utility.Infrastructure.Features.FileIngestion.Parsing;

/// <summary>
/// Streams XML using forward-only XmlReader. Each child element of the record-level XPath
/// becomes one row. Nested elements are flattened to "Parent_Child" keys.
/// Default XPath "/*/*" = children of root element.
/// </summary>
public sealed class XmlFileParser : IFileParser
{
    public async IAsyncEnumerable<Dictionary<string, string>> ParseAsync(
        Stream stream, string fileName, ParseOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var settings = new XmlReaderSettings
        {
            Async              = true,
            IgnoreWhitespace   = true,
            IgnoreComments     = true,
            DtdProcessing      = DtdProcessing.Ignore    // Security: never process DTDs
        };

        using var reader = XmlReader.Create(stream, settings);

        // Determine record-level element name from XPath "/*/*" → depth 2
        // We use a simple depth-based approach: record elements are at depth determined by XPath segments
        int recordDepth = options.XmlRecordXPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;

        int currentDepth = 0;

        while (await reader.ReadAsync() && !ct.IsCancellationRequested)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                currentDepth++;
                if (currentDepth == recordDepth)
                {
                    var row = await ReadRecordElementAsync(reader, ct);
                    yield return row;
                    currentDepth--; // ReadRecordElement consumed the end element
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                currentDepth--;
            }
        }
    }

    private static async Task<Dictionary<string, string>> ReadRecordElementAsync(
        XmlReader reader, CancellationToken ct)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Read attributes of record element
        if (reader.HasAttributes)
        {
            reader.MoveToFirstAttribute();
            do { row[$"@{reader.LocalName}"] = reader.Value; }
            while (reader.MoveToNextAttribute());
            reader.MoveToElement();
        }

        if (reader.IsEmptyElement) return row;

        await ReadChildrenAsync(reader, row, string.Empty, ct);
        return row;
    }

    private static async Task ReadChildrenAsync(
        XmlReader reader, Dictionary<string, string> row,
        string prefix, CancellationToken ct)
    {
        while (await reader.ReadAsync() && !ct.IsCancellationRequested)
        {
            if (reader.NodeType == XmlNodeType.EndElement) break;

            if (reader.NodeType == XmlNodeType.Element)
            {
                var key = string.IsNullOrEmpty(prefix)
                    ? reader.LocalName
                    : $"{prefix}_{reader.LocalName}";

                if (reader.IsEmptyElement) { row[key] = string.Empty; continue; }

                // Check if it has child elements or just text
                var innerText = new System.Text.StringBuilder();
                bool hasChildren = false;

                while (await reader.ReadAsync() && !ct.IsCancellationRequested)
                {
                    if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                        innerText.Append(reader.Value);
                    else if (reader.NodeType == XmlNodeType.Element)
                    {
                        hasChildren = true;
                        // Recurse for nested elements
                        var childKey = $"{key}_{reader.LocalName}";
                        if (!reader.IsEmptyElement)
                        {
                            var childText = new System.Text.StringBuilder();
                            while (await reader.ReadAsync() && !ct.IsCancellationRequested)
                            {
                                if (reader.NodeType == XmlNodeType.Text) childText.Append(reader.Value);
                                else if (reader.NodeType == XmlNodeType.EndElement) break;
                            }
                            row[childKey] = childText.ToString();
                        }
                        else row[childKey] = string.Empty;
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement) break;
                }

                if (!hasChildren) row[key] = innerText.ToString();
            }
        }
    }
}

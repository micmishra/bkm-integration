using System.Runtime.CompilerServices;
using System.Text;
using BKM.Utility.Application.Features.FileIngestion.Interfaces;
using BKM.Utility.Domain.Features.FileIngestion.Entities;
using ExcelDataReader;

namespace BKM.Utility.Infrastructure.Features.FileIngestion.Parsing;

/// <summary>
/// Streams Excel files (.xlsx/.xls) row-by-row using ExcelDataReader.
/// ExcelDataReader reads sequentially without loading the full workbook into memory.
/// All sheets are processed in order; the sheet name is added as "_Sheet" column.
/// </summary>
public sealed class ExcelFileParser : IFileParser
{
    public async IAsyncEnumerable<Dictionary<string, string>> ParseAsync(
        Stream stream, string fileName, ParseOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // ExcelDataReader requires System.Text.Encoding.CodePages on non-Windows
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        using var reader = ext == ".xls"
            ? ExcelReaderFactory.CreateBinaryReader(stream)
            : ExcelReaderFactory.CreateOpenXmlReader(stream);

        do
        {
            string sheetName = reader.Name ?? "Sheet1";
            List<string>? headers = null;
            bool firstRow = true;

            while (reader.Read())
            {
                ct.ThrowIfCancellationRequested();

                if (firstRow)
                {
                    firstRow = false;
                    if (options.HasHeader)
                    {
                        headers = Enumerable.Range(0, reader.FieldCount)
                            .Select(i => reader.GetValue(i)?.ToString() ?? $"Col{i + 1}")
                            .ToList();
                        continue;
                    }
                    headers = Enumerable.Range(0, reader.FieldCount)
                        .Select(i => $"Col{i + 1}")
                        .ToList();
                }

                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["_Sheet"] = sheetName
                };

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var key = i < headers!.Count ? headers[i] : $"Col{i + 1}";
                    row[key] = reader.GetValue(i)?.ToString() ?? string.Empty;
                }

                yield return row;
                await Task.Yield();  // yield to event loop between rows
            }
        }
        while (reader.NextResult());
    }
}

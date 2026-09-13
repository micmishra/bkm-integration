using BKM.Utility.Domain.Features.FileIngestion.Entities;

namespace BKM.Utility.Application.Features.FileIngestion.Interfaces;

/// <summary>
/// Streaming file parser. Yields one row at a time as a string dictionary.
/// Implementations must never load the whole file into memory.
/// </summary>
public interface IFileParser
{
    IAsyncEnumerable<Dictionary<string, string>> ParseAsync(
        Stream stream, string fileName, ParseOptions options, CancellationToken ct = default);
}

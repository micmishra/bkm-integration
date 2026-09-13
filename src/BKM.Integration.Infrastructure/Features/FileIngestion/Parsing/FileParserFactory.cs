using BKM.Integration.Application.Features.FileIngestion.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BKM.Integration.Infrastructure.Features.FileIngestion.Parsing;

public sealed class FileParserFactory(IServiceProvider sp) : IFileParserFactory
{
    public IFileParser GetParser(string fileType) => fileType switch
    {
        "json"  => sp.GetRequiredService<JsonFileParser>(),
        "xml"   => sp.GetRequiredService<XmlFileParser>(),
        "excel" => sp.GetRequiredService<ExcelFileParser>(),
        _       => sp.GetRequiredService<DelimitedFileParser>()  // csv/tsv/pipe/fixed-width
    };
}

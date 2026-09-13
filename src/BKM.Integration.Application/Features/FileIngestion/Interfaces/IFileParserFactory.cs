namespace BKM.Integration.Application.Features.FileIngestion.Interfaces;

public interface IFileParserFactory
{
    IFileParser GetParser(string fileType);
}

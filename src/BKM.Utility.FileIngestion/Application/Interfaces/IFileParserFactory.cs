namespace BKM.Utility.Application.Features.FileIngestion.Interfaces;

public interface IFileParserFactory
{
    IFileParser GetParser(string fileType);
}

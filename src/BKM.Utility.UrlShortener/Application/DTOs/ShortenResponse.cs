namespace BKM.Utility.Application.Features.UrlShortener.DTOs;

public sealed class ShortenResponse
{
    public string ShortUrl { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

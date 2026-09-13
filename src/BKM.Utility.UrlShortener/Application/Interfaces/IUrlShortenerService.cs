using BKM.Utility.Application.Features.UrlShortener.DTOs;

namespace BKM.Utility.Application.Features.UrlShortener.Interfaces;

public interface IUrlShortenerService
{
    Task<ShortenedUrlResult> ShortenAsync(string originalUrl, CancellationToken ct = default);
    Task<string?> ResolveAsync(string code, CancellationToken ct = default);
    string BuildShortUrl(string code);
}

/// <summary>Internal result carrying entity data across the Application boundary.</summary>
public sealed class ShortenedUrlResult
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

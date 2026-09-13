namespace BKM.Integration.Domain.Features.UrlShortener.Entities;

/// <summary>Represents one shortened URL record stored in SQL Server.</summary>
public sealed class ShortenedUrl
{
    /// <summary>Auto-increment primary key — used as the seed for base62 encoding.</summary>
    public long Id { get; set; }

    /// <summary>Base62 short code derived from Id.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The original long URL.</summary>
    public string OriginalUrl { get; set; } = string.Empty;

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

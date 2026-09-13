namespace BKM.Integration.Domain.Features.UrlShortener.Entities;

/// <summary>
/// Maps to the [dbo].[UrlCache] table — exact schema required by
/// Microsoft.Extensions.Caching.SqlServer so EF migrations own the table.
/// </summary>
public sealed class UrlCacheEntry
{
    public string Id { get; set; } = string.Empty;
    public byte[] Value { get; set; } = [];
    public DateTimeOffset ExpiresAtTime { get; set; }
    public long? SlidingExpirationInSeconds { get; set; }
    public DateTimeOffset? AbsoluteExpiration { get; set; }
}

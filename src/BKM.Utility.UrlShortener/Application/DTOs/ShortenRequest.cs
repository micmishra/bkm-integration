using System.ComponentModel.DataAnnotations;

namespace BKM.Utility.Application.Features.UrlShortener.DTOs;

public sealed class ShortenRequest
{
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
}

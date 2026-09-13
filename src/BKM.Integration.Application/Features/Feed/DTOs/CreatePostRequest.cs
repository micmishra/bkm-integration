using System.ComponentModel.DataAnnotations;

namespace BKM.Integration.Application.Features.Feed.DTOs;

public sealed class CreatePostRequest
{
    [Required]
    [MaxLength(500)]
    public string  Body     { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? MediaUrl { get; init; }
}

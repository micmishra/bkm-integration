using System.ComponentModel.DataAnnotations;

namespace BKM.Integration.Application.Features.Feed.DTOs;

public sealed class FeedQueryRequest
{
    [Required]
    public string UserId   { get; init; } = string.Empty;

    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

using System.ComponentModel.DataAnnotations;

namespace BKM.Integration.Application.Features.Feed.DTOs;

public sealed class FollowRequest
{
    [Required]
    public string FolloweeId { get; init; } = string.Empty;
}

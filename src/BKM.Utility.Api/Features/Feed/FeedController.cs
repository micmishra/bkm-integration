using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.Feed.DTOs;
using BKM.Utility.Application.Features.Feed.Interfaces;

namespace BKM.Utility.Api.Features.Feed;

[ApiController]
[Route("api/feed")]
public sealed class FeedController(
    IPushFeedService pushFeedService,
    IPullFeedService pullFeedService) : ControllerBase
{
    /// <summary>Get the pre-computed push feed for a user.</summary>
    /// <remarks>GET /api/feed/push?userId=X&amp;page=1&amp;pageSize=20</remarks>
    [HttpGet("push")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<FeedItemDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPushFeed(
        [FromQuery] FeedQueryRequest request,
        CancellationToken ct)
    {
        var (items, total) = await pushFeedService.GetFeedAsync(request, ct);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return Ok(ApiResponseBuilder<IReadOnlyList<FeedItemDto>>
            .Success(items)
            .WithMessage($"Retrieved {items.Count} push feed items.")
            .WithMeta("totalCount", total)
            .WithMeta("page",       request.Page)
            .WithMeta("pageSize",   pageSize)
            .WithMeta("source",     "push")
            .Build());
    }

    /// <summary>Get the on-demand pull feed for a user.</summary>
    /// <remarks>GET /api/feed/pull?userId=X&amp;page=1&amp;pageSize=20</remarks>
    [HttpGet("pull")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<FeedItemDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPullFeed(
        [FromQuery] FeedQueryRequest request,
        CancellationToken ct)
    {
        var (items, total) = await pullFeedService.GetFeedAsync(request, ct);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return Ok(ApiResponseBuilder<IReadOnlyList<FeedItemDto>>
            .Success(items)
            .WithMessage($"Retrieved {items.Count} pull feed items.")
            .WithMeta("totalCount", total)
            .WithMeta("page",       request.Page)
            .WithMeta("pageSize",   pageSize)
            .WithMeta("source",     "pull")
            .Build());
    }
}

using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.Feed.DTOs;
using BKM.Utility.Application.Features.Feed.Interfaces;

namespace BKM.Utility.Api.Features.Feed;

[ApiController]
[Route("api/follow")]
public sealed class FollowController(IFollowService followService) : ControllerBase
{
    /// <summary>Follow a user.</summary>
    /// <remarks>
    ///     POST /api/follow?followerId=user1
    ///     { "followeeId": "user2" }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Follow(
        [FromBody]  FollowRequest request,
        [FromQuery] string followerId,
        CancellationToken ct)
    {
        await followService.FollowAsync(followerId, request.FolloweeId, ct);

        return Ok(ApiResponseBuilder.Ok($"User '{followerId}' is now following '{request.FolloweeId}'."));
    }

    /// <summary>Unfollow a user.</summary>
    /// <remarks>DELETE /api/follow/{followeeId}?followerId=user1</remarks>
    [HttpDelete("{followeeId}")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unfollow(
        string followeeId,
        [FromQuery] string followerId,
        CancellationToken ct)
    {
        await followService.UnfollowAsync(followerId, followeeId, ct);

        return Ok(ApiResponseBuilder.Ok($"User '{followerId}' unfollowed '{followeeId}'."));
    }

    /// <summary>Get users that the given user is following.</summary>
    /// <remarks>GET /api/follow/followees?userId=user1</remarks>
    [HttpGet("followees")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<string>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowees(
        [FromQuery] string userId,
        CancellationToken ct)
    {
        var followees = await followService.GetFolloweesAsync(userId, ct);

        return Ok(ApiResponseBuilder<IReadOnlyList<string>>
            .Success(followees)
            .WithMessage($"Retrieved {followees.Count} followees for '{userId}'.")
            .WithMeta("totalCount", followees.Count)
            .Build());
    }
}

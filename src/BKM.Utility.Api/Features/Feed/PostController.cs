using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.Feed.DTOs;
using BKM.Utility.Application.Features.Feed.Interfaces;

namespace BKM.Utility.Api.Features.Feed;

[ApiController]
[Route("api/posts")]
public sealed class PostController(IPostService postService) : ControllerBase
{
    /// <summary>Create a new post.</summary>
    /// <remarks>
    ///     POST /api/posts?authorId=user1
    ///     { "body": "Hello world!", "mediaUrl": "https://..." }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType<ApiResponse<PostDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody]  CreatePostRequest request,
        [FromQuery] string authorId,
        CancellationToken ct)
    {
        var dto = await postService.CreateAsync(request, authorId, ct);

        return StatusCode(StatusCodes.Status201Created,
            ApiResponseBuilder<PostDto>
                .Success(dto)
                .WithMessage("Post created successfully.")
                .Build());
    }
}

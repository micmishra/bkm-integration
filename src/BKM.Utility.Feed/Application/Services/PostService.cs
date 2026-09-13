using BKM.Utility.Application.Features.Feed.DTOs;
using BKM.Utility.Application.Features.Feed.Interfaces;
using BKM.Utility.Domain.Features.Feed.Entities;
using BKM.Utility.Domain.Features.Feed.Interfaces;

namespace BKM.Utility.Application.Features.Feed.Services;

/// <summary>
/// Creates posts and fans them out to follower feeds immediately (push-on-write).
/// </summary>
public sealed class PostService(
    IPostRepository postRepository,
    IPushFeedService pushFeedService) : IPostService
{
    public async Task<PostDto> CreateAsync(
        CreatePostRequest request,
        string authorId,
        CancellationToken ct = default)
    {
        var post = new Post
        {
            UserId    = authorId,
            Body      = request.Body,
            MediaUrl  = request.MediaUrl,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await postRepository.SaveAsync(post, ct);

        // Fan out to all followers — awaited directly as part of the write flow
        await pushFeedService.FanOutAsync(saved, ct);

        return new PostDto
        {
            Id        = saved.Id,
            UserId    = saved.UserId,
            Body      = saved.Body,
            MediaUrl  = saved.MediaUrl,
            CreatedAt = saved.CreatedAt
        };
    }
}

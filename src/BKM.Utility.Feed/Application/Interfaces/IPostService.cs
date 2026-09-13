using BKM.Utility.Application.Features.Feed.DTOs;

namespace BKM.Utility.Application.Features.Feed.Interfaces;

public interface IPostService
{
    Task<PostDto> CreateAsync(CreatePostRequest request, string authorId, CancellationToken ct = default);
}

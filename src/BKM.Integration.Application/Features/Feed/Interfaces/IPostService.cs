using BKM.Integration.Application.Features.Feed.DTOs;

namespace BKM.Integration.Application.Features.Feed.Interfaces;

public interface IPostService
{
    Task<PostDto> CreateAsync(CreatePostRequest request, string authorId, CancellationToken ct = default);
}

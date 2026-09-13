using Microsoft.AspNetCore.Mvc;
using BKM.Utility.Application.Features.ApiResponse.Builders;
using BKM.Utility.Application.Features.ApiResponse.Models;
using BKM.Utility.Application.Features.UrlShortener.DTOs;
using BKM.Utility.Application.Features.UrlShortener.Interfaces;

namespace BKM.Utility.Api.Features.UrlShortener;

[ApiController]
public sealed class UrlShortenerController(IUrlShortenerService shortener) : ControllerBase
{
    // ── POST /shorten ──────────────────────────────────────────────────────────

    /// <summary>Shorten a URL.</summary>
    /// <remarks>
    ///     POST /shorten
    ///     { "url": "https://www.example.com/very/long/path" }
    /// </remarks>
    [HttpPost("shorten")]
    [ProducesResponseType<ApiResponse<ShortenResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Shorten(
        [FromBody] ShortenRequest request,
        CancellationToken ct)
    {
        var result = await shortener.ShortenAsync(request.Url, ct);

        var data = new ShortenResponse
        {
            Code     = result.Code,
            ShortUrl = shortener.BuildShortUrl(result.Code)
        };

        return Ok(ApiResponseBuilder.Ok(data, "URL shortened successfully."));
    }

    // ── GET /{code} ────────────────────────────────────────────────────────────

    /// <summary>Redirect to the original URL for the given short code.</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status301MovedPermanently)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Redirect(string code, CancellationToken ct)
    {
        var originalUrl = await shortener.ResolveAsync(code, ct);

        if (originalUrl is null)
        {
            var error = ApiResponseBuilder.Error(
                ErrorCodes.UrlNotFound,
                $"Short code '{code}' was not found.");
            return NotFound(error);
        }

        return RedirectPermanent(originalUrl);
    }
}

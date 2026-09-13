using Microsoft.AspNetCore.Mvc;
using BKM.Integration.Application.Features.ApiResponse.Builders;
using BKM.Integration.Application.Features.ApiResponse.Models;
using BKM.Integration.Application.Features.Encryption.DTOs;
using BKM.Integration.Application.Features.Encryption.Interfaces;

namespace BKM.Integration.Api.Features.Encryption;

[ApiController]
[Route("api/encryption")]
public sealed class EncryptionController(IEncryptionAppService encryptionService) : ControllerBase
{
    // ── POST /api/encryption/encrypt ──────────────────────────────────────────

    /// <summary>Encrypt any text using AES-256-GCM.</summary>
    /// <remarks>
    ///     POST /api/encryption/encrypt
    ///     { "plainText": "Hello, World! 你好 🌍" }
    /// </remarks>
    [HttpPost("encrypt")]
    [ProducesResponseType<ApiResponse<EncryptResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Encrypt([FromBody] EncryptRequest request)
    {
        var data = encryptionService.Encrypt(request);
        return Ok(ApiResponseBuilder.Ok(data, "Text encrypted successfully."));
    }

    // ── POST /api/encryption/decrypt ──────────────────────────────────────────

    /// <summary>Decrypt a cipher package produced by POST /encrypt.</summary>
    /// <remarks>
    ///     POST /api/encryption/decrypt
    ///     { "cipherPackage": "&lt;base64 value from /encrypt&gt;" }
    /// </remarks>
    [HttpPost("decrypt")]
    [ProducesResponseType<ApiResponse<DecryptResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Decrypt([FromBody] DecryptRequest request)
    {
        // CryptographicException is now caught globally — no try/catch needed here
        var data = encryptionService.Decrypt(request);
        return Ok(ApiResponseBuilder.Ok(data, "Text decrypted successfully."));
    }
}

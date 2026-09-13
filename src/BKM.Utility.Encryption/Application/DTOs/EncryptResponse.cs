namespace BKM.Utility.Application.Features.Encryption.DTOs;

public sealed class EncryptResponse
{
    /// <summary>
    /// Base64-encoded cipher package: nonce (12 bytes) + cipher text + GCM tag (16 bytes).
    /// Pass this value directly to POST /decrypt.
    /// </summary>
    public string CipherPackage { get; set; } = string.Empty;

    /// <summary>Algorithm used. Always "AES-256-GCM".</summary>
    public string Algorithm { get; set; } = "AES-256-GCM";
}

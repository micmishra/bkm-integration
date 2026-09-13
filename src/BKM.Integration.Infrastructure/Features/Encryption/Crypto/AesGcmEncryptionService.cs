using System.Security.Cryptography;
using System.Text;
using BKM.Integration.Domain.Features.Encryption.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BKM.Integration.Infrastructure.Features.Encryption.Crypto;

/// <summary>
/// AES-256-GCM authenticated encryption.
///
/// Cipher package layout (all concatenated, then Base64-encoded):
///   [12 bytes nonce] + [N bytes cipher text] + [16 bytes GCM tag]
///
/// Why AES-256-GCM over AES-256-CBC?
///   - GCM provides both confidentiality AND integrity/authenticity in one pass.
///   - No padding oracle attacks (no PKCS#7 padding).
///   - Any byte flip in the cipher text causes decryption to throw — tamper-proof.
///   - Each call generates a fresh random 96-bit nonce — safe for ~2^32 messages
///     under the same key before nonce exhaustion becomes a concern.
///
/// Key configuration (appsettings.json → Encryption:Key):
///   Must be exactly 32 bytes encoded as Base64 (44 characters).
///   Generate: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
/// </summary>
public sealed class AesGcmEncryptionService : IEncryptionService, IDisposable
{
    private const int NonceSize  = 12; // 96-bit nonce — GCM standard
    private const int TagSize    = 16; // 128-bit authentication tag

    private readonly AesGcm _aesGcm;

    public AesGcmEncryptionService(IConfiguration config)
    {
        var keyBase64 = config["Encryption:Key"]
            ?? throw new InvalidOperationException(
                "Encryption:Key is not configured. Add a 32-byte Base64 key to appsettings.json.");

        var keyBytes = Convert.FromBase64String(keyBase64);

        if (keyBytes.Length != 32)
            throw new InvalidOperationException(
                $"Encryption:Key must be exactly 32 bytes (AES-256). Got {keyBytes.Length} bytes.");

        _aesGcm = new AesGcm(keyBytes, TagSize);
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        var nonce      = new byte[NonceSize];
        var cipherText = new byte[plainBytes.Length];
        var tag        = new byte[TagSize];

        RandomNumberGenerator.Fill(nonce);                     // fresh nonce every call
        _aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);

        // Package: nonce | cipherText | tag  →  Base64
        var package = new byte[NonceSize + cipherText.Length + TagSize];
        nonce.CopyTo(package, 0);
        cipherText.CopyTo(package, NonceSize);
        tag.CopyTo(package, NonceSize + cipherText.Length);

        return Convert.ToBase64String(package);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherPackage)
    {
        var package = Convert.FromBase64String(cipherPackage);

        if (package.Length < NonceSize + TagSize)
            throw new CryptographicException("Cipher package is too short to be valid.");

        var nonce      = package[..NonceSize];
        var tag        = package[^TagSize..];
        var cipherText = package[NonceSize..^TagSize];
        var plainBytes = new byte[cipherText.Length];

        // Throws CryptographicException if tag doesn't match (tampered data)
        _aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public void Dispose() => _aesGcm.Dispose();
}

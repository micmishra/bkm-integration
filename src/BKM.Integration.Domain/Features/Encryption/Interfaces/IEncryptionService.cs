namespace BKM.Integration.Domain.Features.Encryption.Interfaces;

/// <summary>
/// Contract for symmetric authenticated encryption/decryption.
/// Implementations must guarantee:
///   - Confidentiality  (cipher text reveals nothing about plain text)
///   - Integrity        (tampered cipher text is rejected with an exception)
///   - Authenticity     (only the holder of the key can produce valid cipher text)
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts <paramref name="plainText"/> and returns a Base64-encoded cipher package
    /// that contains the nonce, cipher text, and GCM authentication tag.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts a Base64-encoded cipher package produced by <see cref="Encrypt"/>.
    /// Throws <see cref="System.Security.Cryptography.CryptographicException"/> if the
    /// package has been tampered with or the key is wrong.
    /// </summary>
    string Decrypt(string cipherPackage);
}

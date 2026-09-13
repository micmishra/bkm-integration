using BKM.Integration.Application.Features.Encryption.DTOs;

namespace BKM.Integration.Application.Features.Encryption.Interfaces;

/// <summary>
/// Application-layer use-case facade for encryption/decryption.
/// Orchestrates the domain <c>IEncryptionService</c> and maps to/from DTOs.
/// </summary>
public interface IEncryptionAppService
{
    EncryptResponse Encrypt(EncryptRequest request);
    DecryptResponse Decrypt(DecryptRequest request);
}

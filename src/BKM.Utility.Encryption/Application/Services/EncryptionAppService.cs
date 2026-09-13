using BKM.Utility.Application.Features.Encryption.DTOs;
using BKM.Utility.Application.Features.Encryption.Interfaces;
using BKM.Utility.Domain.Features.Encryption.Interfaces;

namespace BKM.Utility.Application.Features.Encryption.Services;

/// <summary>
/// Thin orchestration layer: delegates crypto work to the domain <see cref="IEncryptionService"/>
/// and maps results to response DTOs.
/// No business logic lives here — only DTO mapping and validation guards.
/// </summary>
public sealed class EncryptionAppService(IEncryptionService encryptionService) : IEncryptionAppService
{
    public EncryptResponse Encrypt(EncryptRequest request)
    {
        var cipher = encryptionService.Encrypt(request.PlainText);
        return new EncryptResponse { CipherPackage = cipher };
    }

    public DecryptResponse Decrypt(DecryptRequest request)
    {
        var plain = encryptionService.Decrypt(request.CipherPackage);
        return new DecryptResponse { PlainText = plain };
    }
}

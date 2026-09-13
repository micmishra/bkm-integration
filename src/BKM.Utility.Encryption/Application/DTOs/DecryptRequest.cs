using System.ComponentModel.DataAnnotations;

namespace BKM.Utility.Application.Features.Encryption.DTOs;

public sealed class DecryptRequest
{
    /// <summary>Base64-encoded cipher package returned by POST /encrypt.</summary>
    [Required]
    [MinLength(1)]
    public string CipherPackage { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace BKM.Integration.Application.Features.Encryption.DTOs;

public sealed class EncryptRequest
{
    /// <summary>The plain text to encrypt. Can be any UTF-8 string.</summary>
    [Required]
    [MinLength(1)]
    public string PlainText { get; set; } = string.Empty;
}

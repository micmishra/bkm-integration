namespace BKM.Integration.Application.Features.Encryption.DTOs;

public sealed class DecryptResponse
{
    /// <summary>The recovered plain text.</summary>
    public string PlainText { get; set; } = string.Empty;
}

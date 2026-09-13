using System.ComponentModel.DataAnnotations;

namespace BKM.Utility.Application.Features.Email.DTOs;

public sealed class UpsertEmailTemplateRequest
{
    [Required, MaxLength(200)]
    public string  Name        { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string  Subject     { get; set; } = string.Empty;

    [Required]
    public string  Body        { get; set; } = string.Empty;

    public bool    IsHtml      { get; set; } = true;
    public bool    IsActive    { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }
}

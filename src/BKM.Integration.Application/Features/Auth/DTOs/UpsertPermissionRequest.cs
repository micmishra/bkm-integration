using System.ComponentModel.DataAnnotations;

namespace BKM.Integration.Application.Features.Auth.DTOs;

public sealed class UpsertPermissionRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Required, MaxLength(100)]
    public string Resource { get; set; } = "";

    [Required, MaxLength(100)]
    public string Action { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }
}

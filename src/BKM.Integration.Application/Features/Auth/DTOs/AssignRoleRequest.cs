using System.ComponentModel.DataAnnotations;

namespace BKM.Integration.Application.Features.Auth.DTOs;

public sealed class AssignRoleRequest
{
    [Required]
    public string RoleName { get; set; } = "";
}

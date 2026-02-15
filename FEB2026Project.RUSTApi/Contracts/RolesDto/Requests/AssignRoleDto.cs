using System.ComponentModel.DataAnnotations;

namespace FEB2026Project.RUSTApi.Contracts.RolesDto.Requests
{
    public sealed record AssignRoleDto
    {
        [Required]
        public string UserId { get; init; } = string.Empty;

        [Required]
        public string RoleName { get; init; } = string.Empty;
    }
}

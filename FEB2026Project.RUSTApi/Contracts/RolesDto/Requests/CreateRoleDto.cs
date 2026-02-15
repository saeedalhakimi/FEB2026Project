using System.ComponentModel.DataAnnotations;

namespace FEB2026Project.RUSTApi.Contracts.RolesDto.Requests
{
    public sealed record CreateRoleDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string RoleName { get; init; } = string.Empty;
    }
}

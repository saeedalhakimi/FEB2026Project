using System.ComponentModel.DataAnnotations;

namespace FEB2026Project.RUSTApi.Contracts.RolesDto.Requests
{
    public sealed record UpdateRoleDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string RoleNewName { get; init; } = string.Empty;
    }
}

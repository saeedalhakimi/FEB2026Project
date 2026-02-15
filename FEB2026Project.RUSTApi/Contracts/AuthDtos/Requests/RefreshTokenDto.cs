using System.ComponentModel.DataAnnotations;

namespace FEB2026Project.RUSTApi.Contracts.AuthDtos.Requests
{
    public sealed record RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; init; } = string.Empty;
    }
}

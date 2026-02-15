namespace FEB2026Project.RUSTApi.Application.Services.JWTServices
{
    public record ResponseWithTokensDto
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? Message { get; set; }
    }
}

namespace FEB2026Project.RUSTApi.Appilcation.Services.JWTServices
{
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryInMinutes { get; set; }
        public int RefreshTokenExpiryInDays { get; set; } // For refresh token
    }
}

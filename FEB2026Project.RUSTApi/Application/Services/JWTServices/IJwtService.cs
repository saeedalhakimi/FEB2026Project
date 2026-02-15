using FEB2026Project.RUSTApi.Data.ContextModel;

namespace FEB2026Project.RUSTApi.Application.Services.JWTServices
{
    public interface IJwtService
    {
        string GenerateAccessToken(ApplicationUser User, List<string> roles);
        string GenerateRefreshToken();
        DateTime GetRefreshTokenExpiryDate();
    }
}

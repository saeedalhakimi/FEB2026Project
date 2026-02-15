namespace FEB2026Project.RUSTApi.Application.Services.AuthenticationServices.Commands
{
    public sealed record RefreshTokenCommand(
        string RefreshToken,
        string CorrelationId
    );
}

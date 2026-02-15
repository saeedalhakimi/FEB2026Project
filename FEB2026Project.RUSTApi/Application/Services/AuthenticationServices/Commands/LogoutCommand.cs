namespace FEB2026Project.RUSTApi.Application.Services.AuthenticationServices.Commands
{
    public sealed record LogoutCommand(
        string RefreshToken,
        string CorrelationId
    );
}

namespace FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices.Commands
{
    public sealed record LoginUserCommand(
        string Email,
        string Password,
        string CorrelationId
    );
    
}

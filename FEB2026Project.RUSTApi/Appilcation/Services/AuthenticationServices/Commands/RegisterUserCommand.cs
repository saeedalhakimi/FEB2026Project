namespace FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices.Commands
{
    /// <summary>
    /// Command to register a new user.
    /// Carries normalized and validated data.
    /// </summary>
    public sealed record RegisterUserCommand
    (
        string Email,
        string Password,
        string CorrelationId
    );
}

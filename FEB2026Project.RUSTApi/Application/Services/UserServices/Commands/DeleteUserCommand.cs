namespace FEB2026Project.RUSTApi.Application.Services.UserServices.Commands
{
    public sealed record DeleteUserCommand(
        string UserId,
        string CorrelationId
    );
}

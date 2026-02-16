namespace FEB2026Project.RUSTApi.Application.Services.UserServices.Commands
{
    public sealed record UpdateUserCommand(
    string UserId,
    string Email,
    string UserName,
    string CorrelationId
);
}

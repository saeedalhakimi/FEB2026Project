namespace FEB2026Project.RUSTApi.Application.Services.RoleServices.Commands
{
    public sealed record RemoveRoleFromUserCommand(
        string UserId,
        string RoleName,
        string CorrelationId
        );
}

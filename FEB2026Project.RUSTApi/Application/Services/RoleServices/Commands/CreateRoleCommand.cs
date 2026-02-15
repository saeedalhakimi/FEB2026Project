namespace FEB2026Project.RUSTApi.Application.Services.RoleServices.Commands
{
    public sealed record CreateRoleCommand(
        string RoleName,
        string CorrelationId
        );
}

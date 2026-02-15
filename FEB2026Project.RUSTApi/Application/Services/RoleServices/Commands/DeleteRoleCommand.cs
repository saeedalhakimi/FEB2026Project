namespace FEB2026Project.RUSTApi.Application.Services.RoleServices.Commands
{
    public sealed record DeleteRoleCommand(
        string RoleId,
        string CorrelationId
        );
}

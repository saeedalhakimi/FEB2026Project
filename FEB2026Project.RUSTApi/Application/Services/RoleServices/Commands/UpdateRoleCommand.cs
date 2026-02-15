namespace FEB2026Project.RUSTApi.Application.Services.RoleServices.Commands
{
    public sealed record UpdateRoleCommand(
        string RoleId,
        string RoleNewName,
        string CorrelationId
        );
   
}

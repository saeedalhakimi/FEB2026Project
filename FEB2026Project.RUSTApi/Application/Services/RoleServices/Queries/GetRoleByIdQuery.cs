namespace FEB2026Project.RUSTApi.Application.Services.RoleServices.Queries
{
    public sealed record GetRoleByIdQuery(
        string RoleId,
        string CorrelationId 
        );
}

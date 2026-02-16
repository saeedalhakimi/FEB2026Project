namespace FEB2026Project.RUSTApi.Application.Services.UserServices.Queries
{
    public sealed record GetUserByIdQuery(
        string UserId,
        string CorrelationId
    );
}

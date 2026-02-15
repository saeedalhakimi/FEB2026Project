using FEB2026Project.RUSTApi.Application.Operations;
using FEB2026Project.RUSTApi.Application.Services.RoleServices.Commands;
using FEB2026Project.RUSTApi.Application.Services.RoleServices.Queries;
using FEB2026Project.RUSTApi.Contracts.RolesDto.Responses;
using Microsoft.AspNetCore.Identity;
using System.ClientModel.Primitives;

namespace FEB2026Project.RUSTApi.Application.Services.RoleServices
{
    public interface IRoleService
    {
        Task<OperationResult<IReadOnlyList<RoleDto>>> GetRolesQueryHandler(GetRolesQuery query, CancellationToken cancellationToken);
        Task<OperationResult<RoleDto>> GetRoleByIdQueryHandler(GetRoleByIdQuery query, CancellationToken cancellationToken);
        Task<OperationResult<RoleDto>> CreateRoleCommandHandler(CreateRoleCommand command, CancellationToken cancellationToken);
        Task<OperationResult<RoleDto>> UpdateRoleCommandHandler(UpdateRoleCommand command, CancellationToken cancellationToken);
        Task<OperationResult<bool>> DeleteRoleCommandHandler(DeleteRoleCommand command, CancellationToken cancellationToken);
        Task<OperationResult<bool>> AssignRoleToUserCommandHandler(AssignRoleToUserCommand command, CancellationToken cancellationToken);
        Task<OperationResult<bool>> RemoveRoleFromUserCommandHandler(RemoveRoleFromUserCommand command, CancellationToken cancellationToken);
    }
}

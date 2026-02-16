using FEB2026Project.RUSTApi.Application.Operations;
using FEB2026Project.RUSTApi.Application.Services.UserServices.Commands;
using FEB2026Project.RUSTApi.Application.Services.UserServices.Queries;
using FEB2026Project.RUSTApi.Contracts.UsersDto.Responses;

namespace FEB2026Project.RUSTApi.Application.Services.UserServices
{
    public interface IUserService
    {
        Task<OperationResult<IReadOnlyList<UserDto>>> GetUsersQueryHandler(GetUsersQuery query, CancellationToken cancellationToken);
        Task<OperationResult<UserDto>> GetUserByIdQueryHandler(GetUserByIdQuery query, CancellationToken cancellationToken);
        //Task<OperationResult<UserDto>> CreateUserCommandHandler(CreateUserCommand command, CancellationToken cancellationToken);
        //Task<OperationResult<UserDto>> UpdateUserCommandHandler(UpdateUserCommand command, CancellationToken cancellationToken);
        Task<OperationResult<bool>> DeleteUserCommandHandler(DeleteUserCommand command, CancellationToken cancellationToken);
    }
}

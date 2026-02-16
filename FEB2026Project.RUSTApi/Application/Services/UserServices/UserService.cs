using FEB2026Project.RUSTApi.Application.Errors;
using FEB2026Project.RUSTApi.Application.Operations;
using FEB2026Project.RUSTApi.Application.Services.ErrorHandlingServices;
using FEB2026Project.RUSTApi.Application.Services.UserServices.Commands;
using FEB2026Project.RUSTApi.Application.Services.UserServices.Queries;
using FEB2026Project.RUSTApi.Contracts.UsersDto.Responses;
using FEB2026Project.RUSTApi.Data.ContextModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FEB2026Project.RUSTApi.Application.Services.UserServices
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(ILogger<UserService> logger, IErrorHandlingService errorHandlingService, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _errorHandlingService = errorHandlingService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<OperationResult<bool>> DeleteUserCommandHandler(DeleteUserCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(command.UserId);
                if (user == null)
                {
                    var errorMessage = $"User with ID {command.UserId} not found.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<bool>
                        .Failure(
                            ErrorCode.NotFound,
                            errorMessage,
                            correlationId: command.CorrelationId);
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errorMessage = $"Failed to delete user with ID {command.UserId}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<bool>
                        .Failure(
                            ErrorCode.ResourceDeletionFailed,
                            errorMessage,
                            correlationId: command.CorrelationId);
                }

                return OperationResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<bool>(
                        ex,
                        nameof(DeleteUserCommandHandler),
                        command.CorrelationId);
            }
        }
        public async Task<OperationResult<UserDto>> GetUserByIdQueryHandler(GetUserByIdQuery query, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(query.UserId);
                if (user == null)
                {
                    var errorMessage = $"User with ID {query.UserId} not found.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<UserDto>
                        .Failure(
                            ErrorCode.NotFound, 
                            errorMessage, 
                            correlationId: query.CorrelationId);
                }

                var roles = (await _userManager.GetRolesAsync(user!)).ToList();

                var dto = new UserDto(user.Id, user.Email!, user.UserName!, roles);

                return OperationResult<UserDto>.Success(dto);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<UserDto>(
                        ex,
                        nameof(GetUserByIdQueryHandler),
                        query.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<UserDto>(
                        ex,
                        nameof(GetUserByIdQueryHandler),
                        query.CorrelationId);
            }
        }
        public async Task<OperationResult<IReadOnlyList<UserDto>>> GetUsersQueryHandler(GetUsersQuery query, CancellationToken cancellationToken)
        {
            try
            {
                var users = await _userManager.Users
                    .AsNoTracking()
                    .Select(u => new UserDto(u.Id, u.Email!, u.UserName!))
                    .ToListAsync(cancellationToken);

                return OperationResult<IReadOnlyList<UserDto>>.Success(users);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<IReadOnlyList<UserDto>>(
                        ex,
                        nameof(GetUsersQueryHandler),
                        query.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<IReadOnlyList<UserDto>>(
                        ex,
                        nameof(GetUsersQueryHandler),
                        query.CorrelationId);
            }
        }
    }
}

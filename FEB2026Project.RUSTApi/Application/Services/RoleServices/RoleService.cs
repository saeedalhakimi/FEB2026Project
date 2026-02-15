using FEB2026Project.RUSTApi.Application.Errors;
using FEB2026Project.RUSTApi.Application.Operations;
using FEB2026Project.RUSTApi.Application.Services.ErrorHandlingServices;
using FEB2026Project.RUSTApi.Application.Services.RoleServices.Commands;
using FEB2026Project.RUSTApi.Application.Services.RoleServices.Queries;
using FEB2026Project.RUSTApi.Contracts.RolesDto.Responses;
using FEB2026Project.RUSTApi.Data.ContextModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FEB2026Project.RUSTApi.Application.Services.RoleServices
{
    public class RoleService : IRoleService
    {
        private readonly ILogger<RoleService> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public RoleService(ILogger<RoleService> logger, RoleManager<IdentityRole> roleManager, IErrorHandlingService errorHandlingService, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _roleManager = roleManager;
            _errorHandlingService = errorHandlingService;
            _userManager = userManager;
        }

        public async Task<OperationResult<bool>> AssignRoleToUserCommandHandler(AssignRoleToUserCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(command.UserId);
                if (user == null) 
                { 
                    var errorMessage = $"User with ID {command.UserId} not found.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<bool>.Failure(
                        new Error(
                            ErrorCode.NotFound, 
                            errorMessage, 
                            correlationId: command.CorrelationId));
                }

                var roleExists = await _roleManager.RoleExistsAsync(command.RoleName);
                if (!roleExists) 
                {
                    var errorMessage = $"Role with name '{command.RoleName}' not found.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<bool>.Failure(
                        new Error(
                            ErrorCode.NotFound, 
                            errorMessage, 
                            correlationId: command.CorrelationId));
                }

                var result = await _userManager.AddToRoleAsync(user, command.RoleName);
                if (!result.Succeeded)
                {
                    var errorMessage = $"Failed to assign role '{command.RoleName}' to user with ID {command.UserId}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    _logger.LogError(errorMessage);
                    return OperationResult<bool>.Failure(
                        new Error(
                            ErrorCode.AssignmentFailed, 
                            errorMessage, 
                            correlationId: command.CorrelationId));
                }

                return OperationResult<bool>.Success(true);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<bool>(
                        ex,
                        nameof(AssignRoleToUserCommandHandler),
                        command.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<bool>(
                        ex,
                        nameof(AssignRoleToUserCommandHandler),
                        command.CorrelationId);
            }
        }

        public async Task<OperationResult<RoleDto>> CreateRoleCommandHandler(CreateRoleCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var exists = await _roleManager.RoleExistsAsync(command.RoleName);
                if (exists) 
                {
                    var errorMessage = $"Role with name '{command.RoleName}' already exists.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<RoleDto>.Failure(
                        new Error(
                            ErrorCode.Conflict, 
                            errorMessage, 
                            correlationId: command.CorrelationId));
                }

                var role = new IdentityRole(command.RoleName);
                var result = await _roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    var errorMessage = $"Failed to create role '{command.RoleName}'. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    _logger.LogError(errorMessage);
                    return OperationResult<RoleDto>.Failure(
                        new Error(
                            ErrorCode.ResourceCreationFailed, 
                            errorMessage, 
                            correlationId: command.CorrelationId));
                }

                var dto = new RoleDto(role.Id, role.Name!);

                return OperationResult<RoleDto>.Success(dto);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<RoleDto>(
                        ex,
                        nameof(CreateRoleCommandHandler),
                        command.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<RoleDto>(
                        ex,
                        nameof(CreateRoleCommandHandler),
                        command.CorrelationId);
            }
        }

        public async Task<OperationResult<bool>> DeleteRoleCommandHandler(DeleteRoleCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(command.RoleId);
                if (role == null)
                {
                    var errorMessage = $"Role with ID {command.RoleId} not found.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<bool>.Failure(
                        new Error(
                            ErrorCode.NotFound,
                            errorMessage,
                            correlationId: command.CorrelationId));
                }

                var result = await _roleManager.DeleteAsync(role);
                if (!result.Succeeded)
                {
                    var errorMessage = $"Failed to delete role with ID {command.RoleId}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    _logger.LogError(errorMessage);
                    return OperationResult<bool>.Failure(
                        new Error(
                            ErrorCode.ResourceDeletionFailed,
                            errorMessage,
                            correlationId: command.CorrelationId));
                }

                return OperationResult<bool>.Success(true);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<bool>(
                        ex,
                        nameof(DeleteRoleCommandHandler),
                        command.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<bool>(
                        ex,
                        nameof(DeleteRoleCommandHandler),
                        command.CorrelationId);
            }
        }

        public async Task<OperationResult<RoleDto>> GetRoleByIdQueryHandler(GetRoleByIdQuery query, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(query.RoleId);
                if (role == null)
                {
                    var errorMessage = $"Role with ID {query.RoleId} not found.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<RoleDto>.Failure(
                        new Error(
                            ErrorCode.NotFound, 
                            errorMessage, 
                            correlationId: query.CorrelationId));
                }

                var dto = new RoleDto(role.Id, role.Name!);

                return OperationResult<RoleDto>
                    .Success(dto);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<RoleDto>(
                        ex, 
                        nameof(GetRoleByIdQueryHandler), 
                        query.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<RoleDto>(
                        ex, 
                        nameof(GetRoleByIdQueryHandler), 
                        query.CorrelationId);
            }
        }

        public async Task<OperationResult<IReadOnlyList<RoleDto>>> GetRolesQueryHandler(GetRolesQuery query, CancellationToken cancellationToken)
        {
            try
            {
                var roles = await _roleManager.Roles
                    .AsNoTracking()
                    .Select(r => new RoleDto(r.Id, r.Name!))
                    .ToListAsync(cancellationToken);

                return OperationResult<IReadOnlyList<RoleDto>>
                    .Success(roles);

            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<IReadOnlyList<RoleDto>>(
                        ex, 
                        nameof(GetRolesQueryHandler), 
                        query.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<IReadOnlyList<RoleDto>>(
                        ex, 
                        nameof(GetRolesQueryHandler),
                        query.CorrelationId);
            }
        }

        public async Task<OperationResult<bool>> RemoveRoleFromUserCommandHandler(RemoveRoleFromUserCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(command.UserId);
                if (user == null)
                {
                    var errorMessage = $"User with ID {command.UserId} not found.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<bool>.Failure(
                        new Error(
                            ErrorCode.NotFound,
                            errorMessage,
                            correlationId: command.CorrelationId));
                }

                var result = await _userManager.RemoveFromRoleAsync(user, command.RoleName);
                if (!result.Succeeded)
                {
                    var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to remove role '{command.RoleName}' from user with ID {command.UserId}. Errors: {errorMessage}");
                    return OperationResult<bool>.Failure(
                        new Error(ErrorCode.ValidationError,
                            errorMessage,
                            correlationId: command.CorrelationId));
                }

                return OperationResult<bool>.Success(true);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<bool>(
                        ex,
                        nameof(RemoveRoleFromUserCommandHandler),
                        command.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<bool>(
                        ex,
                        nameof(RemoveRoleFromUserCommandHandler),
                        command.CorrelationId);
            }
        }

        public async Task<OperationResult<RoleDto>> UpdateRoleCommandHandler(UpdateRoleCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(command.RoleId);
                if (role == null)
                {
                    var errorMessage = $"Role with ID {command.RoleId} not found.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<RoleDto>.Failure(
                        new Error(
                            ErrorCode.NotFound, 
                            errorMessage, 
                            correlationId: command.CorrelationId));
                }

                role.Name = command.RoleNewName;
                var result = await _roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    var errorMessage = $"Failed to update role with ID {command.RoleId}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    _logger.LogError(errorMessage);
                    return OperationResult<RoleDto>.Failure(
                        new Error(
                            ErrorCode.ResourceUpdateFailed, 
                            errorMessage, 
                            correlationId: command.CorrelationId));
                }

                var dto = new RoleDto(role.Id, role.Name!);

                return OperationResult<RoleDto>.Success(dto);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<RoleDto>(
                        ex,
                        nameof(UpdateRoleCommandHandler),
                        command.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<RoleDto>(
                        ex,
                        nameof(UpdateRoleCommandHandler),
                        command.CorrelationId);
            }
        }
    }
}

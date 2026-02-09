using FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Appilcation.Services.ErrorHandlingServices;
using FEB2026Project.RUSTApi.Appilcation.Services.JWTServices;
using FEB2026Project.RUSTApi.Application.Errors;
using FEB2026Project.RUSTApi.Application.Operations;
using FEB2026Project.RUSTApi.Data;
using FEB2026Project.RUSTApi.Data.ContextModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly DataContext _dataContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtService _jwtService;
        public AuthenticationService(ILogger<AuthenticationService> logger, IErrorHandlingService errorHandlingService, DataContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IJwtService jwtService)
        {
            _logger = logger;
            _errorHandlingService = errorHandlingService;
            _dataContext = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
        }
        public async Task<OperationResult<ResponseWithTokensDto>> RegisterUserCommandHandler(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            var correlationId = command.CorrelationId ?? Guid.NewGuid().ToString();
            var operationName = nameof(RegisterUserCommandHandler);

            using (_logger.BeginScope(new Dictionary<string, object>{["CorrelationId"] = correlationId,["Operation"] = operationName}))
            {
                _logger.LogInformation("RegisterUserCommand Handler started for {Email}", command.Email);
                
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogInformation("Checking if user with email {Email} already exists", command.Email);
                    var isExistingUser = await _userManager.FindByEmailAsync(command.Email);
                    if (isExistingUser is not null)
                    {
                        var errorMessage = $"User with email {command.Email} already exists.";
                        _logger.LogWarning(errorMessage);
                        return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.Conflict, details: errorMessage, correlationId: correlationId));
                    }

                    
                    _logger.LogInformation("Creating user entity for {Email}", command.Email);
                    var newUser = new ApplicationUser{ UserName = command.Email, Email = command.Email, IsActive = true };
                    var createUserResult = await _userManager.CreateAsync(newUser, command.Password);
                    if (!createUserResult.Succeeded)
                    {
                        var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create user {Email}. Errors: {Errors}. CorrelationId: {CorrelationId}", command.Email, errors, correlationId);
                        return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.ResourceCreationFailed, details: errors, correlationId: correlationId));
                    }
                    _logger.LogInformation("User entity created successfully for {Email}", command.Email);
                    
                    const string defaultRole = "User";
                    _logger.LogInformation(defaultRole + " role assignment started for {Email}", command.Email);
                    if (!await _roleManager.RoleExistsAsync(defaultRole))
                    {
                        var roleCreationResult = await _roleManager.CreateAsync(new IdentityRole(defaultRole));
                        if (!roleCreationResult.Succeeded)
                        {
                            var errors = string.Join("; ", roleCreationResult.Errors.Select(e => e.Description));
                            _logger.LogError("Failed to create default role {Role}. Errors: {Errors}. CorrelationId: {CorrelationId}", defaultRole, errors, correlationId);
                            return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.ResourceCreationFailed, details: errors, correlationId: correlationId));
                        }
                        _logger.LogInformation("Default role {Role} created successfully", defaultRole);
                    }

                    var roleAssignmentResult = await _userManager.AddToRoleAsync(newUser, defaultRole);
                    if (!roleAssignmentResult.Succeeded)
                    {
                        var errors = string.Join("; ", roleAssignmentResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to assign role {Role} to user {Email}. Errors: {Errors}. CorrelationId: {CorrelationId}", defaultRole, command.Email, errors, correlationId);
                        return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.ResourceCreationFailed, details: errors, correlationId: correlationId));
                    }

                    _logger.LogInformation("Role {Role} assigned successfully to user {Email}", defaultRole, command.Email);

                    _logger.LogInformation("Generating JWT tokens for user: {Username}", command.Email);
                    var roles = new List<string> { defaultRole }; //add more roles if needed
                    var accesstoken = _jwtService.GenerateAccessToken(newUser, roles);

                    _logger.LogInformation("Generating refresh token for userId:{UserId}", newUser.Id);
                    var refreshToken = _jwtService.GenerateRefreshToken();
                    var refreshTokenEntity = new RefreshToken
                    {
                        Token = refreshToken,
                        ExpiryDate = _jwtService.GetRefreshTokenExpiryDate(),
                        IdentityId = newUser.Id
                    };

                    _logger.LogInformation("Storing refresh token for user: {UserId}", newUser.Id);
                    await _dataContext.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
                    await _dataContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("RegisterUserCommand Handler completed successfully");
                    return OperationResult<ResponseWithTokensDto>.Success(new ResponseWithTokensDto
                    {
                        AccessToken = accesstoken,
                        RefreshToken = refreshToken,
                        Message = "User registered successfully."
                    });
                }
                catch (OperationCanceledException ex)
                {
                    return _errorHandlingService.HandleCancelationTokenException<ResponseWithTokensDto>(ex, operationName, correlationId);
                }
                catch (Exception ex)
                {
                    return _errorHandlingService.HandleUnknownExceptions<ResponseWithTokensDto>(ex, operationName, correlationId);
                }
            }

        }
    }
}

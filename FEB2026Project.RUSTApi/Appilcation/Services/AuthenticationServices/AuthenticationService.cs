using FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Appilcation.Services.ErrorHandlingServices;
using FEB2026Project.RUSTApi.Appilcation.Services.JWTServices;
using FEB2026Project.RUSTApi.Application.Operations;
using FEB2026Project.RUSTApi.Application.Errors;
using FEB2026Project.RUSTApi.Data.ContextModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FEB2026Project.RUSTApi.Data;

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
        public async Task<OperationResult<ResponseWithTokensDto>> LoginUserCommandHandler(LoginUserCommand command, CancellationToken cancellationToken)
        {
            var correlationId = command.CorrelationId ?? Guid.NewGuid().ToString();
            var operationName = nameof(LoginUserCommandHandler);
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["Operation"] = operationName }))
            {
                _logger.LogInformation("LoginUserCommand Handler started for {Email}", command.Email);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogInformation("Finding user by email: {Email}", command.Email);
                    var user = await _userManager.FindByEmailAsync(command.Email);
                    if (user is null)
                    {
                        var errorMessage = "Invalid username or password.";
                        _logger.LogWarning(errorMessage);
                        return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.NotFound, details: errorMessage, correlationId: correlationId));
                    }

                    if (!user.IsActive)
                    {
                        _logger.LogWarning("User {user} is no longer active.", command.Email);
                        return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.Unauthorized, details: $"Access to '{operationName}' was denied due to insufficient permissions.", correlationId: correlationId));
                    }

                    if (await _userManager.IsLockedOutAsync(user))
                    {
                        _logger.LogWarning($"Account is locked.. Contact Admin..");
                        return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.Unauthorized, details: $"User '{user}' is currently locked. Please try again later or contact support.", correlationId: correlationId));
                    }

                    _logger.LogInformation("Verifying password for user: {Username}", command.Email);
                    if (!await _userManager.CheckPasswordAsync(user, command.Password))
                    {
                        await _userManager.AccessFailedAsync(user);
                        var attemptsLeft = _userManager.Options.Lockout.MaxFailedAccessAttempts - await _userManager.GetAccessFailedCountAsync(user);
                        var errorMessage = attemptsLeft > 0
                        ? $"Invalid username or password. You have {attemptsLeft} more attempt(s) before your account gets locked."
                        : "Your account has been locked due to multiple failed login attempts. Please try again later or contact support.";

                        _logger.LogWarning(errorMessage);
                        return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.Unauthorized, details: errorMessage, correlationId: correlationId));
                    }

                    _logger.LogInformation("Retrieving roles for user: {UserId}", user.Id);
                    var roles = (await _userManager.GetRolesAsync(user)).ToList();

                    _logger.LogInformation("Generating tokens for user: {UserId}", user.Id);
                    var token = _jwtService.GenerateAccessToken(user, roles);
                    var refreshToken = _jwtService.GenerateRefreshToken();
                    var refreshTokenEntity = new RefreshToken
                    {
                        Token = refreshToken,
                        ExpiryDate = _jwtService.GetRefreshTokenExpiryDate(), // Use the new method
                        IdentityId = user.Id
                    };

                    _logger.LogInformation("Storing refresh token for user: {UserId}", user.Id);
                    await _dataContext.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
                    await _dataContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("LoginUserCommand Handler completed successfully for {Email}", command.Email);
                    return OperationResult<ResponseWithTokensDto>.Success(new ResponseWithTokensDto
                    {
                        AccessToken = token,
                        RefreshToken = refreshToken,
                        Message = "Login successful."
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

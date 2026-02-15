using FEB2026Project.RUSTApi.Application.Errors;
using FEB2026Project.RUSTApi.Application.Operations;
using FEB2026Project.RUSTApi.Application.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Application.Services.ErrorHandlingServices;
using FEB2026Project.RUSTApi.Application.Services.JWTServices;
using FEB2026Project.RUSTApi.Data;
using FEB2026Project.RUSTApi.Data.ContextModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FEB2026Project.RUSTApi.Application.Services.AuthenticationServices
{
    public class AuthenticationService : IAuthenticationService
    {
        private const string DefaultRole = "User";

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

        // -------------------- REGISTER --------------------
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

                    if (await _userManager.FindByEmailAsync(command.Email) is { } existing)
                    {
                        var errorMessage = $"User with email {existing.Email} already exists.";
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

                    // Role is assumed to be seeded
                    var roleResult = await _userManager.AddToRoleAsync(newUser, DefaultRole); 
                    if (!roleResult.Succeeded)
                    {
                        await _userManager.DeleteAsync(newUser);

                        _logger.LogError("Failed to assign role '{Role}' to user {Email}. Errors: {Errors}. CorrelationId: {CorrelationId}", DefaultRole, command.Email, string.Join("; ", roleResult.Errors.Select(e => e.Description)), correlationId);
                        return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.ResourceCreationFailed, details: $"Failed to assign role '{DefaultRole}' to user.", correlationId: correlationId));
                    }

                    _logger.LogInformation("Generating JWT tokens for user: {Username}", command.Email);
                    var roles = new List<string> { DefaultRole }; //add more roles if needed
                    var accesstoken = _jwtService.GenerateAccessToken(newUser, roles);
                    var refreshToken = _jwtService.GenerateRefreshToken();

                    // STORE REFRESH TOKEN (OWN TRANSACTION)
                    await using var transaction =
                        await _dataContext.Database.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        await _dataContext.RefreshTokens.AddAsync(new RefreshToken
                        {
                            Token = refreshToken,
                            IdentityId = newUser.Id,
                            ExpiryDate = _jwtService.GetRefreshTokenExpiryDate()
                        }, cancellationToken);

                        await _dataContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);

                        //COMPENSATION: Identity cleanup
                        await _userManager.DeleteAsync(newUser);
                        throw;
                    }

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

        // -------------------- LOGIN --------------------
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
                    var validation = await ValidateUserCanAuthenticate<ResponseWithTokensDto>(user, correlationId, operationName);
                    if (!validation.IsSuccess) return validation;

                    _logger.LogInformation("Verifying password for user: {Username}", command.Email);
                    if (!await _userManager.CheckPasswordAsync(user!, command.Password))
                    {
                        var attemptsLeft = _userManager.Options.Lockout.MaxFailedAccessAttempts - await _userManager.GetAccessFailedCountAsync(user!);
                        var errorMessage = attemptsLeft > 0
                        ? $"Invalid username or password. You have {attemptsLeft} more attempt(s) before your account gets locked."
                        : "Your account has been locked due to multiple failed login attempts. Please try again later or contact support.";

                        _logger.LogWarning(errorMessage);
                        return OperationResult<ResponseWithTokensDto>.Failure(new Error(ErrorCode.Unauthorized, details: errorMessage, correlationId: correlationId));
                    }

                    await _userManager.ResetAccessFailedCountAsync(user!);

                    _logger.LogInformation("Retrieving roles for user: {UserId}", user!.Id);
                    var roles = (await _userManager.GetRolesAsync(user)).ToList();

                    _logger.LogInformation("Generating tokens for user: {UserId}", user.Id);
                    var token = _jwtService.GenerateAccessToken(user, roles);
                    var refreshToken = _jwtService.GenerateRefreshToken();

                    // Store refresh token atomically
                    await using var transaction =
                        await _dataContext.Database.BeginTransactionAsync(cancellationToken);

                    await _dataContext.RefreshTokens.AddAsync(new RefreshToken
                    {
                        Token = refreshToken,
                        IdentityId = user.Id,
                        ExpiryDate = _jwtService.GetRefreshTokenExpiryDate()
                    }, cancellationToken);

                    await _dataContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);


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

        // -------------------- REFRESH TOKEN --------------------
        public async Task<OperationResult<ResponseWithTokensDto>> RefreshTokenCommandHandler(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            var correlationId = command.CorrelationId ?? Guid.NewGuid().ToString();
            var operationName = nameof(RefreshTokenCommandHandler);
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["Operation"] = operationName }))
            {
                _logger.LogInformation("RefreshTokenCommand Handler started for RefreshToken");
                await using var transaction =
                    await _dataContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Find token by value ONLY
                    var refreshToken = await _dataContext.RefreshTokens
                        .FirstOrDefaultAsync(rt => rt.Token == command.RefreshToken, cancellationToken);

                    if (refreshToken == null)
                    {
                        _logger.LogWarning("Refresh token not found.");
                        return OperationResult<ResponseWithTokensDto>.Failure(
                            new Error(ErrorCode.Unauthorized,
                                "Invalid refresh token.",
                                correlationId));
                    }

                    // REUSE DETECTION 
                    if (refreshToken.IsUsed || refreshToken.IsRevoked || refreshToken.ExpiryDate <= DateTime.UtcNow)
                    {
                        _logger.LogWarning(
                            "Refresh token reuse detected for UserId: {UserId}",
                            refreshToken.IdentityId);

                        // Revoke ALL refresh tokens for this user
                        var userTokens = await _dataContext.RefreshTokens
                            .Where(rt => rt.IdentityId == refreshToken.IdentityId && !rt.IsRevoked)
                            .ToListAsync(cancellationToken);

                        foreach (var token in userTokens)
                        {
                            token.IsUsed = true;
                            token.IsRevoked = true;
                        }

                        await _dataContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        return OperationResult<ResponseWithTokensDto>.Failure(
                            new Error(ErrorCode.Unauthorized,
                                "Refresh token reuse detected. Please log in again.",
                                correlationId));
                    }

                    _logger.LogInformation("Marking refresh token as used.");
                    var user = await _userManager.FindByIdAsync(refreshToken.IdentityId);
                    var validation = await ValidateUserCanAuthenticate<ResponseWithTokensDto>(user, correlationId, operationName);
                    if (!validation.IsSuccess) return validation;

                    // Rotate refresh token (one-time use)
                    refreshToken.IsUsed = true;
                    refreshToken.IsRevoked = true;

                    var roles = (await _userManager.GetRolesAsync(user!)).ToList();

                    _logger.LogInformation("Generating new access token and refresh token for user: {UserId}", user!.Id);
                    var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
                    var newRefreshToken = _jwtService.GenerateRefreshToken();
                    await _dataContext.RefreshTokens.AddAsync(new RefreshToken
                    {
                        Token = newRefreshToken,
                        IdentityId = user!.Id,
                        ExpiryDate = _jwtService.GetRefreshTokenExpiryDate()
                    }, cancellationToken);

                    await _dataContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("RefreshTokenCommand Handler completed successfully for user: {UserId}", user.Id);
                    return OperationResult<ResponseWithTokensDto>.Success(new ResponseWithTokensDto
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = newRefreshToken,
                        Message = "Token refreshed successfully."
                    });
                }
                catch (OperationCanceledException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return _errorHandlingService.HandleCancelationTokenException<ResponseWithTokensDto>(ex, operationName, correlationId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return _errorHandlingService.HandleUnknownExceptions<ResponseWithTokensDto>(ex, operationName, correlationId);
                }
            }
        }

        // -------------------- LOGOUT (ALL SESSIONS) --------------------
        public async Task<OperationResult<bool>> LogoutCommandHandler(LogoutCommand command, CancellationToken cancellationToken)
        {
            var correlationId = command.CorrelationId ?? Guid.NewGuid().ToString();
            var operationName = nameof(LogoutCommandHandler);
            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["Operation"] = operationName }))
            {
                _logger.LogInformation("LogoutCommand Handler started for RefreshToken: {RefreshToken}", command.RefreshToken);
                
                
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentToken = await _dataContext.RefreshTokens
                        .FirstOrDefaultAsync(rt => rt.Token == command.RefreshToken, 
                            cancellationToken);

                    if (currentToken == null)
                    {
                        return OperationResult<bool>.Failure(
                            new Error(ErrorCode.NotFound, "Refresh token not found.", correlationId));
                    }

                    var tokens = await _dataContext.RefreshTokens
                        .Where(rt => rt.IdentityId == currentToken.IdentityId && !rt.IsRevoked)
                        .ToListAsync(cancellationToken);

                    _logger.LogInformation("Revoking current refresh token");
                    foreach (var token in tokens)
                    {
                        token.IsRevoked = true;
                        token.IsUsed = true;
                    }

                    await _dataContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("LogoutCommand Handler completed successfully for RefreshToken: {RefreshToken}", command.RefreshToken);
                    return OperationResult<bool>.Success(true);
                }
                catch (OperationCanceledException ex)
                {
                    return _errorHandlingService.HandleCancelationTokenException<bool>(ex, operationName, correlationId);
                }
                catch (Exception ex)
                {
                    return _errorHandlingService.HandleUnknownExceptions<bool>(ex, operationName, correlationId);
                }
            }
        }


        // -------------------- HELPER METHODS --------------------
        private async Task<OperationResult<T>> ValidateUserCanAuthenticate<T>(ApplicationUser? user, string correlationId, string operationName)
        {
            if (user is null)
            {
                var errorMessage = "Invalid username or password.";
                _logger.LogWarning(errorMessage);
                return OperationResult<T>.Failure(new Error(ErrorCode.Unauthorized, details: errorMessage, correlationId: correlationId));
            }

            if (!user!.IsActive)
            {
                _logger.LogWarning("User {user} is no longer active.", user.Email);
                return OperationResult<T>.Failure(new Error(ErrorCode.Unauthorized, details: $"Access to '{operationName}' was denied due to insufficient permissions.", correlationId: correlationId));
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning($"Account is locked.. Contact Admin..");
                return OperationResult<T>.Failure(new Error(ErrorCode.Unauthorized, details: $"User '{user}' is currently locked. Please try again later or contact support.", correlationId: correlationId));
            }

            return OperationResult<T>.Success(default!);
        }
    }
}

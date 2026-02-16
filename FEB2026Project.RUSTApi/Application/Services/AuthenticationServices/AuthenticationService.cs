using FEB2026Project.RUSTApi.Application.Errors;
using FEB2026Project.RUSTApi.Application.Operations;
using FEB2026Project.RUSTApi.Application.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Application.Services.ErrorHandlingServices;
using FEB2026Project.RUSTApi.Application.Services.JWTServices;
using FEB2026Project.RUSTApi.Data;
using FEB2026Project.RUSTApi.Data.ContextModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        public async Task<OperationResult<ResponseWithTokensDto>> RegisterUserCommandHandler(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            try
            {
                if (await _userManager.FindByEmailAsync(command.Email) is { } existing)
                {
                    var errorMessage = $"User with email {existing.Email} already exists.";
                    _logger.LogWarning(errorMessage);
                    return OperationResult<ResponseWithTokensDto>
                        .Failure(
                            new Error(
                                ErrorCode.Conflict, 
                                details: errorMessage, 
                                correlationId: command.CorrelationId));
                }

                var newUser = new ApplicationUser { UserName = command.Email, Email = command.Email, IsActive = true };
                var createUserResult = await _userManager.CreateAsync(newUser, command.Password);
                if (!createUserResult.Succeeded)
                {
                    var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user {Email}. Errors: {Errors}. CorrelationId: {CorrelationId}", 
                        command.Email, errors, command.CorrelationId);
                    return OperationResult<ResponseWithTokensDto>
                        .Failure(
                            new Error(
                                ErrorCode.ResourceCreationFailed, 
                                details: errors, 
                                correlationId: command.CorrelationId));
                }

                var roleResult = await _userManager.AddToRoleAsync(newUser, DefaultRole);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(newUser);
                    _logger.LogError("Failed to assign role '{Role}' to user {Email}. Errors: {Errors}. CorrelationId: {CorrelationId}", 
                        DefaultRole, command.Email, string.Join("; ", roleResult.Errors.Select(e => e.Description)), command.CorrelationId);
                    return OperationResult<ResponseWithTokensDto>
                        .Failure(
                            new Error(
                                ErrorCode.ResourceCreationFailed, 
                                details: $"Failed to assign role '{DefaultRole}' to user.", 
                                correlationId: command.CorrelationId));
                }

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

                return OperationResult<ResponseWithTokensDto>.Success(new ResponseWithTokensDto
                {
                    AccessToken = accesstoken,
                    RefreshToken = refreshToken,
                    Message = "User registered successfully."
                });

            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<ResponseWithTokensDto>(
                        ex, 
                        nameof(RegisterUserCommandHandler), 
                        command.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<ResponseWithTokensDto>(
                        ex,
                        nameof(RegisterUserCommandHandler),
                        command.CorrelationId);
            }
        }
        public async Task<OperationResult<ResponseWithTokensDto>> LoginUserCommandHandler(LoginUserCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(command.Email);
                var validation = await ValidateUserCanAuthenticate<ResponseWithTokensDto>(
                    user, command.CorrelationId, nameof(LoginUserCommandHandler));
                if (!validation.IsSuccess) return validation;

                if (!await _userManager.CheckPasswordAsync(user!, command.Password))
                {
                    var attemptsLeft = _userManager.Options.Lockout.MaxFailedAccessAttempts - await _userManager.GetAccessFailedCountAsync(user!);
                    var errorMessage = attemptsLeft > 0
                    ? $"Invalid username or password. You have {attemptsLeft} more attempt(s) before your account gets locked."
                    : "Your account has been locked due to multiple failed login attempts. Please try again later or contact support.";

                    _logger.LogWarning(errorMessage);
                    return OperationResult<ResponseWithTokensDto>
                        .Failure(
                            new Error(
                                ErrorCode.Unauthorized, 
                                details: errorMessage, 
                                correlationId: command.CorrelationId));
                }

                await _userManager.ResetAccessFailedCountAsync(user!);

                var roles = (await _userManager.GetRolesAsync(user!)).ToList();
                var token = _jwtService.GenerateAccessToken(user!, roles);
                var refreshToken = _jwtService.GenerateRefreshToken();

                await using var transaction =
                        await _dataContext.Database.BeginTransactionAsync(cancellationToken);

                await _dataContext.RefreshTokens.AddAsync(new RefreshToken
                {
                    Token = refreshToken,
                    IdentityId = user!.Id,
                    ExpiryDate = _jwtService.GetRefreshTokenExpiryDate()
                }, cancellationToken);

                await _dataContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return OperationResult<ResponseWithTokensDto>
                    .Success(
                        new ResponseWithTokensDto
                        {
                            AccessToken = token,
                            RefreshToken = refreshToken,
                            Message = "Login successful."
                        });
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<ResponseWithTokensDto>(
                        ex,
                        nameof(LoginUserCommandHandler),
                        command.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<ResponseWithTokensDto>(
                        ex,
                        nameof(LoginUserCommandHandler),
                        command.CorrelationId);
            }
        }
        public async Task<OperationResult<ResponseWithTokensDto>> RefreshTokenCommandHandler(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            await using var transaction =
                    await _dataContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Find token by value ONLY
                var refreshToken = await _dataContext.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == command.RefreshToken, cancellationToken);

                if (refreshToken == null)
                {
                    _logger.LogWarning("Refresh token not found.");
                    return OperationResult<ResponseWithTokensDto>.Failure(
                        new Error(ErrorCode.Unauthorized,
                            "Invalid refresh token.",
                            command.CorrelationId));
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
                            command.CorrelationId));
                }

                var user = await _userManager.FindByIdAsync(refreshToken.IdentityId);
                var validation = await ValidateUserCanAuthenticate<ResponseWithTokensDto>(
                                    user, command.CorrelationId, nameof(RefreshTokenCommandHandler));
                if (!validation.IsSuccess) return validation;

                // Rotate refresh token (one-time use)
                refreshToken.IsUsed = true;
                refreshToken.IsRevoked = true;

                var roles = (await _userManager.GetRolesAsync(user!)).ToList();

                var newAccessToken = _jwtService.GenerateAccessToken(user!, roles);
                var newRefreshToken = _jwtService.GenerateRefreshToken();
                await _dataContext.RefreshTokens.AddAsync(new RefreshToken
                {
                    Token = newRefreshToken,
                    IdentityId = user!.Id,
                    ExpiryDate = _jwtService.GetRefreshTokenExpiryDate()
                }, cancellationToken);

                await _dataContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return OperationResult<ResponseWithTokensDto>
                    .Success(new ResponseWithTokensDto
                        {
                            AccessToken = newAccessToken,
                            RefreshToken = newRefreshToken,
                            Message = "Token refreshed successfully."
                        });
            }
            catch (OperationCanceledException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return _errorHandlingService
                    .HandleCancelationTokenException<ResponseWithTokensDto>(
                        ex, 
                        nameof(RefreshTokenCommandHandler), 
                        command.CorrelationId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return _errorHandlingService
                    .HandleUnknownExceptions<ResponseWithTokensDto>(
                        ex,
                        nameof(RefreshTokenCommandHandler), 
                        command.CorrelationId);
            }
        }
        public async Task<OperationResult<bool>> LogoutCommandHandler(LogoutCommand command, CancellationToken cancellationToken)
        {
            try
            {
                var currentToken = await _dataContext.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == command.RefreshToken,
                        cancellationToken);

                if (currentToken == null)
                {
                    return OperationResult<bool>.Failure(
                        new Error(
                            ErrorCode.NotFound, 
                            "Refresh token not found.", 
                            command.CorrelationId));
                }

                var tokens = await _dataContext.RefreshTokens
                    .Where(rt => rt.IdentityId == currentToken.IdentityId && !rt.IsRevoked)
                    .ToListAsync(cancellationToken);

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.IsUsed = true;
                }

                await _dataContext.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true);
            }
            catch (OperationCanceledException ex)
            {
                return _errorHandlingService
                    .HandleCancelationTokenException<bool>(
                        ex, 
                        nameof(LogoutCommandHandler), 
                        command.CorrelationId);
            }
            catch (Exception ex)
            {
                return _errorHandlingService
                    .HandleUnknownExceptions<bool>(
                        ex,
                        nameof(LogoutCommandHandler), 
                        command.CorrelationId);
            }
        }


        // -------------------- HELPER METHODS --------------------
        private async Task<OperationResult<T>> ValidateUserCanAuthenticate<T>(ApplicationUser? user, string correlationId, string operationName)
        {
            if (user is null)
            {
                var errorMessage = "Invalid username or password.";
                _logger.LogWarning(errorMessage);
                return OperationResult<T>
                    .Failure(
                        new Error(
                            ErrorCode.Unauthorized, 
                            details: errorMessage, 
                            correlationId: correlationId));
            }

            if (!user!.IsActive)
            {
                _logger.LogWarning("User {user} is no longer active.", user.Email);
                return OperationResult<T>
                    .Failure(
                        new Error(
                            ErrorCode.Unauthorized, 
                            details: $"Access to '{operationName}' was denied due to insufficient permissions.", 
                            correlationId: correlationId));
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning($"Account is locked.. Contact Admin..");
                return OperationResult<T>
                    .Failure(
                        new Error(
                            ErrorCode.Unauthorized,   
                            details: $"User '{user}' is currently locked. Please try again later or contact support.", 
                            correlationId: correlationId));
            }

            return OperationResult<T>.Success(default!);
        }
    }
}

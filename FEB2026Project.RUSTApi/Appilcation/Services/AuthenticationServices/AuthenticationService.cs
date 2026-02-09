using FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Appilcation.Services.ErrorHandlingServices;
using FEB2026Project.RUSTApi.Application.Operations;

namespace FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        public AuthenticationService(ILogger<AuthenticationService> logger, IErrorHandlingService errorHandlingService)
        {
            _logger = logger;
            _errorHandlingService = errorHandlingService;
        }
        public async Task<OperationResult<string>> RegisterUserCommandHandler(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            var correlationId = command.CorrelationId ?? Guid.NewGuid().ToString();
            var operationName = nameof(RegisterUserCommandHandler);

            using (_logger.BeginScope(new Dictionary<string, object>{["CorrelationId"] = correlationId,["Operation"] = operationName}))
            {
                _logger.LogInformation("RegisterUserCommand Handler started for {Email}", command.Email);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    //Step 1: Check if user already exists

                    //Step 2: Hash password
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(command.Password);

                    //Setp 3: Create user entity with roles and permissions


                    //Step 5: create user profile

                    //Step 6: Save to database

                    //Step 7: implement JWT token generation

                    _logger.LogInformation("RegisterUserCommand Handler completed successfully");
                    return OperationResult<string>.Success("User registered successfully");
                }
                catch (OperationCanceledException ex)
                {
                    return _errorHandlingService.HandleCancelationTokenException<string>(ex, operationName, correlationId);
                }
                catch (Exception ex)
                {
                    return _errorHandlingService.HandleUnknownExceptions<string>(ex, operationName, correlationId);
                }
            }

        }
    }
}

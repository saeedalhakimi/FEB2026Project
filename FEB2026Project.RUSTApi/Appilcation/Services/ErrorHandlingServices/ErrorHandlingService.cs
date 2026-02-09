using FEB2026Project.RUSTApi.Application.Errors;
using FEB2026Project.RUSTApi.Application.Operations;

namespace FEB2026Project.RUSTApi.Appilcation.Services.ErrorHandlingServices
{
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;
        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger;
        }

        public OperationResult<T> HandleCancelationTokenException<T>(OperationCanceledException ex, string operation, string correlationId)
        {
            _logger.LogWarning(ex, "Operation {Operation} was canceled. CorrelationId: {CorrelationId}", operation, correlationId);
            return OperationResult<T>.Failure(Error.FromException(ErrorCode.OperationCanceled, ex, operation, correlationId));
        }

        public OperationResult<T> HandleUnknownExceptions<T>(Exception ex, string operation, string correlationId)
        {
            _logger.LogError(ex, "An unexpected error occurred during {Operation}. CorrelationId: {CorrelationId}", operation, correlationId);
            return OperationResult<T>.Failure(Error.FromException(ErrorCode.UnknownError, ex, operation, correlationId));
        }
    }
}

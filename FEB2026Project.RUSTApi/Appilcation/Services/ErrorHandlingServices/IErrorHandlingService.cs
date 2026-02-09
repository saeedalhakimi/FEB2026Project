using FEB2026Project.RUSTApi.Application.Operations;

namespace FEB2026Project.RUSTApi.Appilcation.Services.ErrorHandlingServices
{
    public interface IErrorHandlingService
    {
        OperationResult<T> HandleUnknownExceptions<T>(Exception ex, string operation, string correlationId);
        OperationResult<T> HandleCancelationTokenException<T>(OperationCanceledException ex, string operation, string correlationId);
    }
}

using DOT10.Business.Application.Errors;
using DOT10.Business.Application.Operations;
using FEB2026Project.RUSTApi.Errors;
using Microsoft.AspNetCore.Mvc;

namespace FEB2026Project.RUSTApi.Models
{
    public class BaseController<T> : Controller
    {
        protected IActionResult HandleResult<TResult>(OperationResult<TResult> result, string correlationId)
        {
            try
            {
                if (!result.HasErrors())
                {
                    var error = new Error(ErrorCode.UnknownError,
                        message: "An unknown error occurred.",
                        details: "No error details provided.",
                        correlationId: correlationId);

                    return CreateErrorResponse(error);
                }

                var correlationID = result.Errors.First().CorrelationId ?? correlationId;
                return CreateErrorResponse(result.Errors.First(), result.Timestamp);
            }
            catch (Exception ex)
            {
                var error = Error.FromException(ErrorCode.InternalServerError, ex, "HandleResult", correlationId);
                return CreateErrorResponse(error);
            }
        }

        private IActionResult CreateErrorResponse(Error error, DateTime? timestamp = null)
        {
            var apiError = new ErrorResponse
            {
                Timestamp = timestamp ?? DateTime.UtcNow,
                CorrelationId = error.CorrelationId ?? HttpContext.TraceIdentifier,
                Errors = new List<string> { error.Message},
                ErrorsDetails = string.IsNullOrEmpty(error.Details) ? null : new List<string> { error.Details },
                ErrorCodes = new List<string> { error.Code.ToString() },
                StatusCode = error.HttpStatus,
                StatusPhrase = error.Severity,
                Path = HttpContext.Request.Path,
                Method = HttpContext.Request.Method,
                Detail = $"An error occurred while processing the request: {error.Message}"
            };

            return StatusCode(error.HttpStatus, apiError);
        }
    }
}

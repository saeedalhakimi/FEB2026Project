using Asp.Versioning;
using FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices;
using FEB2026Project.RUSTApi.Contracts.AuthDtos.Requests;
using FEB2026Project.RUSTApi.Filters;
using FEB2026Project.RUSTApi.Mappers.AuthMappers;
using FEB2026Project.RUSTApi.Models;
using FEB2026Project.RUSTApi.URLs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FEB2026Project.RUSTApi.Controllers.V1.Authentications
{
    [ApiVersion("1.0")]
    [Route(ApiRoutes.AuthRoutes.BaseRoute)]
    [ApiController]
    public class AuthController : BaseController<AuthController>
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthenticationService _authService;
        public AuthController(ILogger<AuthController> logger, IAuthenticationService authenticationService) : base(logger)
        {
            _logger = logger;
            _authService = authenticationService;
        }

        [HttpPost(ApiRoutes.AuthRoutes.Register, Name = "Register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserDto dto, CancellationToken cancellationToken)
        {
            var operationName = nameof(RegisterUser);
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using (_logger.BeginScope("Operation: {OperationName}, CorrelationId: {CorrelationId}", operationName, correlationId))
            {
                _logger.LogInformation("Request received for {Operation} at {Path}", operationName, HttpContext.Request.Path);
                var command = AuthMappers.ToRegisterUserCommand(dto, correlationId);
                var result = await _authService.RegisterUserCommandHandler(command, cancellationToken);
                if (!result.IsSuccess) return HandleResult(result, correlationId);

                _logger.LogInformation("Request for {Operation} completed successfully at {Path}", operationName, HttpContext.Request.Path);
                return Ok(result.Data);
            }
        }

        [HttpPost(ApiRoutes.AuthRoutes.Login, Name = "Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<IActionResult> Login([FromBody] LoginUserDto dto, CancellationToken cancellationToken)
        {
            var operationName = nameof(Login);
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            using (_logger.BeginScope("Operation: {OperationName}, CorrelationId: {CorrelationId}", operationName, correlationId))
            {
                _logger.LogInformation("Request received for {Operation} at {Path}", operationName, HttpContext.Request.Path);
                var command = AuthMappers.ToLoginUserCommand(dto, correlationId);
                var result = await _authService.LoginUserCommandHandler(command, cancellationToken);
                if (!result.IsSuccess) return HandleResult(result, correlationId);
                
                _logger.LogInformation("Request for {Operation} completed successfully at {Path}", operationName, HttpContext.Request.Path);
                return Ok(result.Data);
            }
        }
    }
}

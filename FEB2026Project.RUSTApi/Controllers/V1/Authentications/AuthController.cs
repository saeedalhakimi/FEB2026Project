using Asp.Versioning;
using FEB2026Project.RUSTApi.Application.Services.AuthenticationServices;
using FEB2026Project.RUSTApi.Application.Services.JWTServices;
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
        private readonly IAuthenticationService _handler;

        public AuthController(ILogger<AuthController> logger, IAuthenticationService authenticationService)
            : base(logger)
        {
            _handler = authenticationService;
        }

        [HttpPost(ApiRoutes.AuthRoutes.Register, Name = "Register")]
        [ProducesResponseType(typeof(ResponseWithTokensDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<ActionResult<ResponseWithTokensDto>> RegisterUser([FromBody] RegisterUserDto dto, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var command = AuthMappers.ToRegisterUserCommand(dto, CorrelationId);
            var result = await _handler.RegisterUserCommandHandler(command, cancellationToken);
            if (!result.IsSuccess) return HandleResult(result);

            return Created(string.Empty, result.Data);
        }

        [HttpPost(ApiRoutes.AuthRoutes.Login, Name = "Login")]
        [ProducesResponseType(typeof(ResponseWithTokensDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<ActionResult<ResponseWithTokensDto>> Login([FromBody] LoginUserDto dto, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope(); 
            
            var command = AuthMappers.ToLoginUserCommand(dto, CorrelationId);
            var result = await _handler.LoginUserCommandHandler(command, cancellationToken);
            if (!result.IsSuccess) return HandleResult(result);
                
            return Ok(result.Data);
        }

        [HttpPost(ApiRoutes.AuthRoutes.RefreshToken, Name = "RefreshToken")]
        [ProducesResponseType(typeof(ResponseWithTokensDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<ActionResult<ResponseWithTokensDto>> RefreshToken([FromBody] RefreshTokenDto dto, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var command = AuthMappers.ToRefreshTokenCommand(dto, CorrelationId);
            var result = await _handler.RefreshTokenCommandHandler(command, cancellationToken);
            if (!result.IsSuccess) return HandleResult(result);

            return Ok(result.Data);
        }

        [HttpPost(ApiRoutes.AuthRoutes.Logout, Name = "Logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var command = AuthMappers.ToLogoutCommand(dto, CorrelationId);
            var result = await _handler.LogoutCommandHandler(command, cancellationToken);
            if (!result.IsSuccess) return HandleResult(result);

            return NoContent();
        }
    }
}

using Asp.Versioning;
using FEB2026Project.RUSTApi.Application.Services.UserServices;
using FEB2026Project.RUSTApi.Application.Services.UserServices.Commands;
using FEB2026Project.RUSTApi.Application.Services.UserServices.Queries;
using FEB2026Project.RUSTApi.Contracts.UsersDto.Responses;
using FEB2026Project.RUSTApi.Filters;
using FEB2026Project.RUSTApi.Models;
using FEB2026Project.RUSTApi.URLs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FEB2026Project.RUSTApi.Controllers.V1.UsersControllers
{
    [ApiVersion("1.0")]
    [Route(ApiRoutes.UserRoutes.BaseRoute)]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : BaseController<UsersController>
    {
        private readonly IUserService _handler;

        public UsersController(ILogger<UsersController> logger, IUserService userService) : base(logger)
        {
            _handler = userService;
        }

        [HttpGet(Name = "GetAllUsers")]
        [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAllUsers(CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var query = new GetUsersQuery(CorrelationId);
            var result = await _handler.GetUsersQueryHandler(query, cancellationToken);
            if (!result.IsSuccess) return HandleResult(result);
            return Ok(result.Data);
        }

        [HttpGet(ApiRoutes.UserRoutes.GetUserById, Name = "GetUserById")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateGuid("id")]
        public async Task<ActionResult<UserDto>> GetUserByID([FromRoute] string id, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();
            var query = new GetUserByIdQuery(id, CorrelationId);
            var result = await _handler.GetUserByIdQueryHandler(query, cancellationToken);
            if (!result.IsSuccess) return HandleResult(result);
            return Ok(result.Data);
        }

        [HttpDelete(ApiRoutes.UserRoutes.GetUserById, Name = "DeleteUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateGuid("id")]
        public async Task<IActionResult> DeleteUser([FromRoute] string id, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();
            var command = new DeleteUserCommand(id, CorrelationId);
            var result = await _handler.DeleteUserCommandHandler(command, cancellationToken);
            if (!result.IsSuccess) return HandleResult(result);
            return NoContent();

        }
    }
}

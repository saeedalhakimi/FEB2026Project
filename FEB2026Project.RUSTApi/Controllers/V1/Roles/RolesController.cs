using Asp.Versioning;
using FEB2026Project.RUSTApi.Application.Services.JWTServices;
using FEB2026Project.RUSTApi.Application.Services.RoleServices;
using FEB2026Project.RUSTApi.Application.Services.RoleServices.Commands;
using FEB2026Project.RUSTApi.Application.Services.RoleServices.Queries;
using FEB2026Project.RUSTApi.Contracts.RolesDto.Requests;
using FEB2026Project.RUSTApi.Contracts.RolesDto.Responses;
using FEB2026Project.RUSTApi.Filters;
using FEB2026Project.RUSTApi.Models;
using FEB2026Project.RUSTApi.URLs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FEB2026Project.RUSTApi.Controllers.V1.Roles
{
    [ApiVersion("1.0")]
    [Route(ApiRoutes.RoleRoutes.BaseRoute)]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class RolesController : BaseController<RolesController>
    {
        private readonly IRoleService _handler;

        public RolesController(ILogger<RolesController> logger, IRoleService handler) : base(logger)
        {
            _handler = handler;
        }

        [HttpGet(Name = "GetAllRoles")]
        [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAllRoles(CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope(); 
            
            var query = new GetRolesQuery(CorrelationId);
            var result = await _handler.GetRolesQueryHandler(query, cancellationToken);
            if (!result.IsSuccess) return HandleResult(result);

            return Ok(result.Data);
        }

        [HttpGet(ApiRoutes.RoleRoutes.GetRoleById, Name = "GetRoleById")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateGuid("id")]
        public async Task<ActionResult<RoleDto>> GetRoleByID([FromRoute] string id, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var query = new GetRoleByIdQuery(id, CorrelationId);
            var result = await _handler.GetRoleByIdQueryHandler(query, cancellationToken);
            if (!result.IsSuccess) return HandleResult(result);
            
            return Ok(result.Data);
        }

        // =========================================================
        // CREATE ROLE
        // =========================================================
        [HttpPost(Name = "CreateRole")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto dto, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var command = new CreateRoleCommand(dto.RoleName, CorrelationId);
            var result = await _handler.CreateRoleCommandHandler(command, cancellationToken);

            if (!result.IsSuccess) return HandleResult(result);

            return CreatedAtRoute(
                "GetRoleById",
                new { id = result.Data!.Id },
                result.Data);
        }

        // =========================================================
        // UPDATE ROLE
        // =========================================================
        [HttpPut(ApiRoutes.RoleRoutes.GetRoleById, Name = "UpdateRole")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        [ValidateGuid("id")]
        public async Task<ActionResult<RoleDto>> UpdateRole([FromRoute] string id, [FromBody] UpdateRoleDto dto, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var command = new UpdateRoleCommand(id, dto.RoleNewName, CorrelationId);
            var result = await _handler.UpdateRoleCommandHandler(command, cancellationToken);

            if (!result.IsSuccess) return HandleResult(result);

            return Ok(result.Data);
        }

        // =========================================================
        // DELETE ROLE
        // =========================================================
        [HttpDelete(ApiRoutes.RoleRoutes.GetRoleById, Name = "DeleteRole")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ValidateGuid("id")]
        public async Task<IActionResult> DeleteRole([FromRoute] string id, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var command = new DeleteRoleCommand(id, CorrelationId);
            var result = await _handler.DeleteRoleCommandHandler(command, cancellationToken);

            if (!result.IsSuccess) return HandleResult(result);

            return NoContent();
        }

        // =========================================================
        // ASSIGN ROLE TO USER
        // =========================================================
        [HttpPost("assign", Name = "AssignRoleToUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ValidateModel]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleDto dto, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var command = new AssignRoleToUserCommand(dto.UserId, dto.RoleName, CorrelationId);
            var result = await _handler.AssignRoleToUserCommandHandler(command, cancellationToken);

            if (!result.IsSuccess) return HandleResult(result);

            return NoContent();
        }

        // =========================================================
        // REMOVE ROLE FROM USER
        // =========================================================
        [HttpPost("remove", Name = "RemoveRoleFromUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] AssignRoleDto dto, CancellationToken cancellationToken)
        {
            using var _ = BeginRequestScope();

            var command = new RemoveRoleFromUserCommand(dto.UserId, dto.RoleName, CorrelationId);
            var result = await _handler.RemoveRoleFromUserCommandHandler(command, cancellationToken);

            if (!result.IsSuccess) return HandleResult(result);

            return NoContent();
        }
    }
}

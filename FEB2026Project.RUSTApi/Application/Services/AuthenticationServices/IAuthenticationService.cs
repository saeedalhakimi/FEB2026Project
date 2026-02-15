using FEB2026Project.RUSTApi.Application.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Application.Services.JWTServices;
using FEB2026Project.RUSTApi.Application.Operations;

namespace FEB2026Project.RUSTApi.Application.Services.AuthenticationServices
{
    public interface IAuthenticationService
    {
            Task<OperationResult<ResponseWithTokensDto>> RegisterUserCommandHandler(RegisterUserCommand command, CancellationToken cancellationToken);
            Task<OperationResult<ResponseWithTokensDto>> LoginUserCommandHandler(LoginUserCommand command, CancellationToken cancellationToken);
            Task<OperationResult<ResponseWithTokensDto>> RefreshTokenCommandHandler(RefreshTokenCommand command, CancellationToken cancellationToken);
            Task<OperationResult<bool>> LogoutCommandHandler(LogoutCommand command, CancellationToken cancellationToken);
    }
}

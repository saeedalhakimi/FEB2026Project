using FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Appilcation.Services.JWTServices;
using FEB2026Project.RUSTApi.Application.Operations;

namespace FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices
{
    public interface IAuthenticationService
    {
            Task<OperationResult<ResponseWithTokensDto>> RegisterUserCommandHandler(RegisterUserCommand command, CancellationToken cancellationToken);
    }
}

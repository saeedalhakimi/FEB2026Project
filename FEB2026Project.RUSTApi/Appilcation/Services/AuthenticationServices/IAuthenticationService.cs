using FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Application.Operations;

namespace FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices
{
    public interface IAuthenticationService
    {
            Task<OperationResult<string>> RegisterUserCommandHandler(RegisterUserCommand command, CancellationToken cancellationToken);
    }
}

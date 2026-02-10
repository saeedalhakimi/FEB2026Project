
using FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Contracts.AuthDtos.Requests;

namespace FEB2026Project.RUSTApi.Mappers.AuthMappers
{
    public static class AuthMappers
    {
        public static RegisterUserCommand ToRegisterUserCommand(RegisterUserDto dto, string correlationId) 
        {
                return new RegisterUserCommand(
                    Email: dto.Email.Trim().ToLowerInvariant(),
                    Password: dto.Password,
                    CorrelationId: correlationId
                ); 
        }

        public static LoginUserCommand ToLoginUserCommand(LoginUserDto dto, string correlationId) 
        {
                return new LoginUserCommand(
                    Email: dto.Email.Trim().ToLowerInvariant(),
                    Password: dto.Password,
                    CorrelationId: correlationId
                );
        }
    }
}

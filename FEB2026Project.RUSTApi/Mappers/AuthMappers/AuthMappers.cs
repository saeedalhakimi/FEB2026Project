
using FEB2026Project.RUSTApi.Appilcation.Services.AuthenticationServices.Commands;
using FEB2026Project.RUSTApi.Contracts.AuthDtos.Requests;

namespace FEB2026Project.RUSTApi.Mappers.AuthMappers
{
    public static class AuthMappers
    {
        public static RegisterUserCommand ToRegisterUserCommand(RegisterUserDto dto, string correlationId) 
        {
                return new RegisterUserCommand(
                    FirstName: dto.FirstName.Trim(),
                    LastName: dto.LastName.Trim(),
                    DateOfBirth: dto.DateOfBirth,
                    Email: dto.Email.Trim().ToLowerInvariant(),
                    Password: dto.Password,
                    CorrelationId: correlationId
                ); 
        }
    }
}

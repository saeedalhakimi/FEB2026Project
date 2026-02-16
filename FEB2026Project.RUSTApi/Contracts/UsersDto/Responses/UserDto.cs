namespace FEB2026Project.RUSTApi.Contracts.UsersDto.Responses
{
    public sealed record UserDto(
        string Id,
        string Username,
        string Email,
        IList<string>? Roles = null
    );
}

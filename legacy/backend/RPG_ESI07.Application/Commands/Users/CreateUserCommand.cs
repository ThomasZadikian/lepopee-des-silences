using MediatR;

namespace RPG_ESI07.Application.Commands.Users;

public record CreateUserCommand(
    string Username, string Email, string PasswordHash
) : IRequest<CreateUserResponse>;

public record CreateUserResponse(int Id, string Message);
using MediatR;

namespace RPG_ESI07.Application.Commands.Users;

public record UpdateUserCommand(
    int Id, string Username, string Email
) : IRequest<UpdateUserResponse>;

public record UpdateUserResponse(bool Success, string Message);
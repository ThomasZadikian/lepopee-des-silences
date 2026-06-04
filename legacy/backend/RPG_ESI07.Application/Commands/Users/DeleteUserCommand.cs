using MediatR;

namespace RPG_ESI07.Application.Commands.Users;

public record DeleteUserCommand(int Id) : IRequest<DeleteUserResponse>;

public record DeleteUserResponse(bool Success, string Message);
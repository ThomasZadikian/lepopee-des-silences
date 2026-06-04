using MediatR;

namespace RPG_ESI07.Application.Commands.Auth;

public record LoginCommand(
string Username,
string Password
) : IRequest<AuthResponse>;
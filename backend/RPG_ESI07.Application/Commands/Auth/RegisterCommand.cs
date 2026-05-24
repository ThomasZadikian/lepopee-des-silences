using MediatR;

namespace RPG_ESI07.Application.Commands.Auth;

public record RegisterCommand(
    string Username,
    string Email,
    string Password) : IRequest<AuthResponse>;

public record AuthResponse(
    bool success,
    string? token,
    bool requiresMfa,
    string message,
    bool requiresMfaSetup = false, 
    int? userId = null
);
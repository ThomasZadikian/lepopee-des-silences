using MediatR;

namespace RPG_ESI07.Application.Commands.Auth;

public record LoginMfaCommand(int UserId, string Code) : IRequest<AuthResponse>;
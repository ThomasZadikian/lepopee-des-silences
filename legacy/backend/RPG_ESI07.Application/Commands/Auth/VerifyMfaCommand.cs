using MediatR;

namespace RPG_ESI07.Application.Commands.Auth;
public record VerifyMfaCommand(int UserId, string MfaToken, string Code) : IRequest<AuthResponse>;


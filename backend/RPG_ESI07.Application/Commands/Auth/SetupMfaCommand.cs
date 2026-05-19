using MediatR;

namespace RPG_ESI07.Application.Commands.Auth;

public record SetupMfaCommand(int UserId, string MfaToken) : IRequest<SetupMfaResponse>;
public record SetupMfaResponse(bool Success, string? QrCodeUri, string? Secret, string Message);
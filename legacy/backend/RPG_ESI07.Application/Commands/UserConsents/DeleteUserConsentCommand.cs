using MediatR;

namespace RPG_ESI07.Application.Commands.UserConsents;

public record DeleteUserConsentCommand(int Id, int RequestingUserId, bool IsAdmin)
    : IRequest<DeleteUserConsentResponse>;

public record DeleteUserConsentResponse(bool Success, string Message);
using MediatR;

namespace RPG_ESI07.Application.Commands.BestiaryUnlocks;

public record DeleteBestiaryUnlockCommand(int Id, int RequestingUserId, bool IsAdmin)
    : IRequest<DeleteBestiaryUnlockResponse>;

public record DeleteBestiaryUnlockResponse(bool Success, string Message);
using MediatR;

namespace RPG_ESI07.Application.Commands.BestiaryUnlocks;

public record UpdateBestiaryUnlockCommand(
    int Id, int PlayerId, int EnemyId, int RequestingUserId, bool IsAdmin
) : IRequest<UpdateBestiaryUnlockResponse>;

public record UpdateBestiaryUnlockResponse(bool Success, string Message);
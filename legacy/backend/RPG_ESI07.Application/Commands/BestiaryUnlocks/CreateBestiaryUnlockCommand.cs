using MediatR;

namespace RPG_ESI07.Application.Commands.BestiaryUnlocks;

public record CreateBestiaryUnlockCommand(
    int PlayerId, int EnemyId
) : IRequest<CreateBestiaryUnlockResponse>;

public record CreateBestiaryUnlockResponse(int Id, string Message);

public record CreateBestiaryUnlockForMeCommand(int EnemyId);
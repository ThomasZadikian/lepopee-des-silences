using MediatR;

namespace RPG_ESI07.Application.Commands.CombatStatss;

public record CreateCombatStatsCommand(
    int PlayerId
) : IRequest<CreateCombatStatsResponse>;

public record CreateCombatStatsResponse(int Id, string Message);
using MediatR;

namespace RPG_ESI07.Application.Commands.CombatStatss;

public record UpdateCombatStatsCommand(
    int Id, int PlayerId, int TotalCombats, int CombatsWon, int CombatsLost,
    long TotalDamageDealt, long TotalDamageTaken, int TotalPlaytimeMinutes,
    int RequestingUserId, bool IsAdmin
) : IRequest<UpdateCombatStatsResponse>;

public record UpdateCombatStatsResponse(bool Success, string Message);
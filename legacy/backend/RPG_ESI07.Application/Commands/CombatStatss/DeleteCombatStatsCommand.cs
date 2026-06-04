using MediatR;

namespace RPG_ESI07.Application.Commands.CombatStatss;

public record DeleteCombatStatsCommand(int Id, int RequestingUserId, bool IsAdmin)
    : IRequest<DeleteCombatStatsResponse>;

public record DeleteCombatStatsResponse(bool Success, string Message);
using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.CombatStatss;

public record GetCombatStatsByIdQuery(int Id, int RequestingUserId, bool IsAdmin)
    : IRequest<GetCombatStatsByIdResponse>;

public record GetCombatStatsByIdResponse(CombatStats CombatStats);
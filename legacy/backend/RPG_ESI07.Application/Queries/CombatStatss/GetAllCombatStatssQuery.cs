using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.CombatStatss;

public record GetAllCombatStatssQuery : IRequest<GetAllCombatStatssResponse>;

public record GetAllCombatStatssResponse(List<CombatStats> Items);
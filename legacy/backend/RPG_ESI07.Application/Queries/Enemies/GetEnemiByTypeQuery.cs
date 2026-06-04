using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Enemies;

public record GetEnemiesByTypeQuery(string Type) : IRequest<GetEnemiesByTypeResponse>;
public record GetEnemiesByTypeResponse(List<Enemy> Items);
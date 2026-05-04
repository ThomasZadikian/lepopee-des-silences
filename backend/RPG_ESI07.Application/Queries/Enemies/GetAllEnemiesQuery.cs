using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Enemies;

public record GetAllEnemiesQuery() : IRequest<GetAllEnemiesResponse>;
public record GetAllEnemiesResponse(List<Enemy> Items);
using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Enemies;

public record GetEnemyByIdQuery(int Id) : IRequest<GetEnemyByIdResponse>;
public record GetEnemyByIdResponse(Enemy Enemy);
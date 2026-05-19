using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Npcs;
public record GetNpcsByTypeQuery(string Type) : IRequest<GetNpcsByTypeResponse>;
public record GetNpcsByTypeResponse(List<Npc> Items);
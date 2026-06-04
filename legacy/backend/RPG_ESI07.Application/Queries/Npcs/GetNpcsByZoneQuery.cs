using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Npcs;

public record GetNpcsByZoneQuery(string Zone) : IRequest<GetNpcsByZoneResponse>;
public record GetNpcsByZoneResponse(List<Npc> Items);
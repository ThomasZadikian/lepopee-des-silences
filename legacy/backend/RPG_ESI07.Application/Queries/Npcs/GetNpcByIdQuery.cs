using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Npcs;

public record GetNpcByIdQuery(int Id) : IRequest<GetNpcByIdResponse>;
public record GetNpcByIdResponse(Npc? Item);
using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.Npcs;

public record GetAllNpcsQuery : IRequest<GetAllNpcsResponse>;
public record GetAllNpcsResponse(List<Npc> Items);
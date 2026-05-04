using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.BestiaryUnlocks;

public record GetAllBestiaryUnlocksQuery(int? UserId = null): IRequest<GetAllBestiaryUnlocksResponse>;
public record GetAllBestiaryUnlocksResponse(List<BestiaryUnlock> Items);
using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.PlayerInventorys;

public record GetAllPlayerInventorysQuery(int? UserId = null) : IRequest<GetAllPlayerInventorysResponse>;
public record GetAllPlayerInventorysResponse(List<PlayerInventory> Items);
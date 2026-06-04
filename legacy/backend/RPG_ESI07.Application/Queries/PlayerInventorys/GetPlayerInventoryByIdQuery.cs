using MediatR;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Application.Queries.PlayerInventorys;

public record GetPlayerInventoryByIdQuery(int Id, int RequestingUserId, bool IsAdmin) : IRequest<GetPlayerInventoryByIdResponse>;

public record GetPlayerInventoryByIdResponse(PlayerInventory playerInventory);
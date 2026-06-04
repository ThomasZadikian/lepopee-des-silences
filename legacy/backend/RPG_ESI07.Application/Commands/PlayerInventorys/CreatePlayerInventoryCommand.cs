using MediatR;

namespace RPG_ESI07.Application.Commands.PlayerInventorys;

public record CreatePlayerInventoryCommand(
    int PlayerId, int ItemId, int Quantity
) : IRequest<CreatePlayerInventoryResponse>;

public record CreatePlayerInventoryResponse(int Id, string Message);
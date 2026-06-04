using MediatR;

namespace RPG_ESI07.Application.Commands.PlayerInventorys;

public record UpdatePlayerInventoryCommand(
    int Id, int PlayerId, int ItemId, int Quantity,
    int RequestingUserId, bool IsAdmin
) : IRequest<UpdatePlayerInventoryResponse>;

public record UpdatePlayerInventoryResponse(bool Success, string Message);
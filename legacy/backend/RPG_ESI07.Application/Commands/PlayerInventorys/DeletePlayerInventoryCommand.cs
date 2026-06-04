using MediatR;

namespace RPG_ESI07.Application.Commands.PlayerInventorys;

public record DeletePlayerInventoryCommand(int Id, int RequestingUserId, bool IsAdmin)
    : IRequest<DeletePlayerInventoryResponse>;

public record DeletePlayerInventoryResponse(bool Success, string Message);